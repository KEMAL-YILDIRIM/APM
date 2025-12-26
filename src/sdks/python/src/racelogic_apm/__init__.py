"""
Racelogic APM SDK for Python applications.

Usage:
    from racelogic_apm import ApmClient

    apm = ApmClient(
        endpoint="https://apm.example.com",
        api_key="your-api-key",
        application_name="my-python-app",
    )

    # Logging
    apm.logger.info("User logged in", user_id=123)
    apm.logger.error("Something failed", exception=e)

    # Metrics
    apm.metrics.counter("user_logins", 1)
    apm.metrics.gauge("active_users", 42)

    # Tracing
    with apm.tracer.start_span("process_order") as span:
        span.set_attribute("order_id", 456)
        # ... do work
"""

from .client import ApmClient
from .logger import ApmLogger
from .metrics import ApmMetrics
from .config import ApmConfig

__all__ = ["ApmClient", "ApmLogger", "ApmMetrics", "ApmConfig"]
__version__ = "1.0.0"
