"""APM Metrics implementation."""

import time
import threading
from typing import Any, Optional
from queue import Queue
from dataclasses import dataclass

import httpx

from .config import ApmConfig


@dataclass
class MetricRecord:
    name: str
    type: str  # "gauge", "counter", "histogram"
    value: float
    timestamp: int
    attributes: dict


class ApmMetrics:
    """Metrics collector for sending metrics to APM Collector."""

    def __init__(self, config: ApmConfig):
        self._config = config
        self._queue: Queue = Queue()
        self._client = httpx.Client(timeout=30.0)
        self._shutdown = False
        self._flush_thread = threading.Thread(target=self._flush_loop, daemon=True)
        self._flush_thread.start()

    def counter(self, name: str, value: int, **attributes: Any) -> None:
        """Record a counter metric (monotonically increasing value)."""
        self._record("counter", name, float(value), attributes)

    def gauge(self, name: str, value: float, **attributes: Any) -> None:
        """Record a gauge metric (point-in-time value)."""
        self._record("gauge", name, value, attributes)

    def histogram(self, name: str, value: float, **attributes: Any) -> None:
        """Record a histogram metric (distribution of values)."""
        self._record("histogram", name, value, attributes)

    def _record(
        self, metric_type: str, name: str, value: float, attributes: dict[str, Any]
    ) -> None:
        record = MetricRecord(
            name=name,
            type=metric_type,
            value=value,
            timestamp=int(time.time() * 1_000_000_000),
            attributes=attributes,
        )
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
        records: list[MetricRecord] = []
        while not self._queue.empty() and len(records) < self._config.batch_size:
            try:
                records.append(self._queue.get_nowait())
            except Exception:
                break

        if not records:
            return

        # Group by metric name
        grouped: dict[str, list[MetricRecord]] = {}
        for record in records:
            if record.name not in grouped:
                grouped[record.name] = []
            grouped[record.name].append(record)

        metrics = []
        for name, recs in grouped.items():
            first = recs[0]
            data_points = [
                {
                    "timeUnixNano": r.timestamp,
                    "asDouble": r.value,
                    "attributes": self._convert_attributes(r.attributes),
                }
                for r in recs
            ]

            if first.type == "counter":
                metrics.append(
                    {
                        "name": name,
                        "sum": {
                            "dataPoints": data_points,
                            "isMonotonic": True,
                            "aggregationTemporality": 2,
                        },
                    }
                )
            else:
                metrics.append({"name": name, "gauge": {"dataPoints": data_points}})

        request = {
            "resourceMetrics": [
                {
                    "resource": {
                        "attributes": [
                            {
                                "key": "service.name",
                                "value": {"stringValue": self._config.application_name},
                            }
                        ]
                    },
                    "scopeMetrics": [
                        {
                            "scope": {"name": "racelogic-apm"},
                            "metrics": metrics,
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
                f"{self._config.endpoint}/v1/metrics",
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
        """Flush all pending metrics."""
        self._flush()

    def shutdown(self) -> None:
        """Shutdown the metrics collector."""
        self._shutdown = True
        self._flush()
        self._client.close()
