"""Main APM Client implementation."""

from typing import Optional
import atexit

from .config import ApmConfig
from .logger import ApmLogger
from .metrics import ApmMetrics


class ApmClient:
    """
    APM Client for Python applications.

    Usage:
        apm = ApmClient(
            endpoint="https://apm.example.com",
            api_key="your-api-key",
            application_name="my-python-app",
        )

        apm.logger.info("Hello, APM!")
        apm.metrics.counter("requests", 1)
    """

    _instance: Optional["ApmClient"] = None

    def __init__(
        self,
        endpoint: str,
        application_name: str,
        api_key: Optional[str] = None,
        application_id: Optional[str] = None,
        environment: str = "development",
        service_version: Optional[str] = None,
        batch_size: int = 100,
        flush_interval_ms: int = 5000,
        **kwargs,
    ):
        """
        Initialize the APM client.

        Args:
            endpoint: The APM Collector endpoint URL
            application_name: Human-readable name for this application
            api_key: Optional API key for authentication
            application_id: Optional unique identifier for this application
            environment: Environment name (e.g., development, staging, production)
            service_version: Version of this application
            batch_size: Number of records to batch before sending
            flush_interval_ms: Interval in milliseconds to flush the buffer
        """
        self._config = ApmConfig(
            endpoint=endpoint,
            application_name=application_name,
            api_key=api_key,
            application_id=application_id,
            environment=environment,
            service_version=service_version,
            batch_size=batch_size,
            flush_interval_ms=flush_interval_ms,
        )

        self._logger = ApmLogger(self._config)
        self._metrics = ApmMetrics(self._config)

        # Register shutdown handler
        atexit.register(self.shutdown)

        # Set as singleton
        ApmClient._instance = self

    @classmethod
    def get_instance(cls) -> "ApmClient":
        """Get the current APM client instance."""
        if cls._instance is None:
            raise RuntimeError("ApmClient not initialized. Create an instance first.")
        return cls._instance

    @property
    def logger(self) -> ApmLogger:
        """Get the logger instance."""
        return self._logger

    @property
    def metrics(self) -> ApmMetrics:
        """Get the metrics instance."""
        return self._metrics

    def flush(self) -> None:
        """Flush all pending telemetry."""
        self._logger.flush()
        self._metrics.flush()

    def shutdown(self) -> None:
        """Shutdown the APM client and flush all pending telemetry."""
        self._logger.shutdown()
        self._metrics.shutdown()

    # Flask integration
    def instrument_flask(self, app) -> None:
        """
        Add APM middleware to a Flask application.

        Usage:
            from flask import Flask
            app = Flask(__name__)
            apm.instrument_flask(app)
        """
        import time

        @app.before_request
        def before_request():
            from flask import g

            g.apm_start_time = time.time()

        @app.after_request
        def after_request(response):
            from flask import g, request

            duration_ms = (time.time() - g.apm_start_time) * 1000
            self._metrics.histogram(
                "http_request_duration_ms",
                duration_ms,
                method=request.method,
                path=request.path,
                status=response.status_code,
            )
            self._logger.info(
                f"{request.method} {request.path}",
                method=request.method,
                path=request.path,
                status_code=response.status_code,
                duration_ms=duration_ms,
            )
            return response

    # Context manager for tracing
    def trace(self, name: str):
        """
        Decorator for tracing a function.

        Usage:
            @apm.trace("process_order")
            def process_order(order_id):
                ...
        """
        import functools
        import time

        def decorator(func):
            @functools.wraps(func)
            def wrapper(*args, **kwargs):
                start = time.time()
                try:
                    result = func(*args, **kwargs)
                    duration_ms = (time.time() - start) * 1000
                    self._metrics.histogram(f"{name}_duration_ms", duration_ms)
                    return result
                except Exception as e:
                    duration_ms = (time.time() - start) * 1000
                    self._logger.error(f"{name} failed", exception=e)
                    self._metrics.histogram(f"{name}_duration_ms", duration_ms)
                    raise

            return wrapper

        return decorator
