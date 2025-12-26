"""Configuration for the APM SDK."""

from dataclasses import dataclass, field
from typing import Optional


@dataclass
class ApmConfig:
    """Configuration options for the APM SDK."""

    # Required
    endpoint: str
    application_name: str

    # Authentication
    api_key: Optional[str] = None
    application_id: Optional[str] = None

    # Environment
    environment: str = "development"
    service_version: Optional[str] = None

    # Batching
    batch_size: int = 100
    flush_interval_ms: int = 5000

    # Retry
    max_retries: int = 3
    retry_delay_ms: int = 1000

    # Additional resource attributes
    resource_attributes: dict = field(default_factory=dict)
