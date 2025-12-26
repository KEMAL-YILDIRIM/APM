"""APM Logger implementation."""

import time
import threading
import traceback
from typing import Any, Optional
from queue import Queue

import httpx

from .config import ApmConfig


SEVERITY_MAP = {
    "trace": (1, "TRACE"),
    "debug": (5, "DEBUG"),
    "info": (9, "INFO"),
    "warn": (13, "WARN"),
    "warning": (13, "WARN"),
    "error": (17, "ERROR"),
    "fatal": (21, "FATAL"),
}


class ApmLogger:
    """Logger for sending log records to APM Collector."""

    def __init__(self, config: ApmConfig):
        self._config = config
        self._queue: Queue = Queue()
        self._client = httpx.Client(timeout=30.0)
        self._shutdown = False
        self._flush_thread = threading.Thread(target=self._flush_loop, daemon=True)
        self._flush_thread.start()

    def trace(self, message: str, **attributes: Any) -> None:
        """Log a trace message."""
        self._log("trace", message, None, attributes)

    def debug(self, message: str, **attributes: Any) -> None:
        """Log a debug message."""
        self._log("debug", message, None, attributes)

    def info(self, message: str, **attributes: Any) -> None:
        """Log an informational message."""
        self._log("info", message, None, attributes)

    def warn(self, message: str, **attributes: Any) -> None:
        """Log a warning message."""
        self._log("warn", message, None, attributes)

    def warning(self, message: str, **attributes: Any) -> None:
        """Log a warning message (alias for warn)."""
        self._log("warn", message, None, attributes)

    def error(
        self,
        message: str,
        exception: Optional[BaseException] = None,
        **attributes: Any,
    ) -> None:
        """Log an error message."""
        self._log("error", message, exception, attributes)

    def fatal(
        self,
        message: str,
        exception: Optional[BaseException] = None,
        **attributes: Any,
    ) -> None:
        """Log a fatal error message."""
        self._log("fatal", message, exception, attributes)

    def _log(
        self,
        level: str,
        message: str,
        exception: Optional[BaseException],
        attributes: dict[str, Any],
    ) -> None:
        severity_number, severity_text = SEVERITY_MAP.get(level, (9, "INFO"))

        body = message
        if exception:
            body = f"{message}\n{traceback.format_exception(type(exception), exception, exception.__traceback__)}"

        record = {
            "timeUnixNano": int(time.time() * 1_000_000_000),
            "severityNumber": severity_number,
            "severityText": severity_text,
            "body": {"stringValue": body},
            "attributes": self._convert_attributes(attributes),
        }

        self._queue.put(record)

        if self._queue.qsize() >= self._config.batch_size:
            self._flush()

    def _convert_attributes(self, attributes: dict[str, Any]) -> list[dict]:
        result = []
        for key, value in attributes.items():
            if value is None:
                continue
            if isinstance(value, str):
                result.append({"key": key, "value": {"stringValue": value}})
            elif isinstance(value, bool):
                result.append({"key": key, "value": {"boolValue": value}})
            elif isinstance(value, int):
                result.append({"key": key, "value": {"intValue": value}})
            elif isinstance(value, float):
                result.append({"key": key, "value": {"doubleValue": value}})
            else:
                result.append({"key": key, "value": {"stringValue": str(value)}})
        return result

    def _flush(self) -> None:
        records = []
        while not self._queue.empty() and len(records) < self._config.batch_size:
            try:
                records.append(self._queue.get_nowait())
            except Exception:
                break

        if not records:
            return

        request = {
            "resourceLogs": [
                {
                    "resource": {
                        "attributes": [
                            {
                                "key": "service.name",
                                "value": {"stringValue": self._config.application_name},
                            },
                            {
                                "key": "service.version",
                                "value": {
                                    "stringValue": self._config.service_version or "1.0.0"
                                },
                            },
                            {
                                "key": "deployment.environment",
                                "value": {"stringValue": self._config.environment},
                            },
                        ]
                    },
                    "scopeLogs": [
                        {
                            "scope": {"name": "racelogic-apm"},
                            "logRecords": records,
                        }
                    ],
                }
            ]
        }

        headers = {"Content-Type": "application/json"}
        if self._config.api_key:
            headers["X-API-Key"] = self._config.api_key
        if self._config.application_id:
            headers["X-Application-Id"] = self._config.application_id

        try:
            self._client.post(
                f"{self._config.endpoint}/v1/logs",
                json=request,
                headers=headers,
            )
        except Exception:
            # Re-queue on failure
            for record in records:
                self._queue.put(record)

    def _flush_loop(self) -> None:
        while not self._shutdown:
            time.sleep(self._config.flush_interval_ms / 1000)
            if not self._shutdown:
                self._flush()

    def flush(self) -> None:
        """Flush all pending log records."""
        self._flush()

    def shutdown(self) -> None:
        """Shutdown the logger and flush pending records."""
        self._shutdown = True
        self._flush()
        self._client.close()
