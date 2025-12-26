# APM Architecture Specification

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Client Applications                             │
├─────────────────────┬─────────────────────┬─────────────────────────────────┤
│    .NET App         │    JS/Node App      │        Python App               │
│  ┌─────────────┐    │  ┌─────────────┐    │     ┌─────────────┐             │
│  │  APM.SDK    │    │  │  apm-sdk    │    │     │  apm_sdk    │             │
│  │  (.NET)     │    │  │  (npm)      │    │     │  (PyPI)     │             │
│  └──────┬──────┘    │  └──────┬──────┘    │     └──────┬──────┘             │
└─────────┼───────────┴─────────┼───────────┴────────────┼────────────────────┘
          │                     │                        │
          │         OTLP/HTTP (JSON/Protobuf)            │
          └─────────────────────┼────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         APM Collector Service                                │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    OTLP Receiver Endpoint                            │    │
│  │                   POST /v1/logs, /v1/metrics, /v1/traces            │    │
│  └──────────────────────────────┬──────────────────────────────────────┘    │
│                                 │                                            │
│  ┌──────────────────────────────▼──────────────────────────────────────┐    │
│  │                    Data Processing Pipeline                          │    │
│  │   • Validation  • Normalization  • Batching  • Enrichment           │    │
│  └──────────────────────────────┬──────────────────────────────────────┘    │
│                                 │                                            │
│  ┌──────────────────────────────▼──────────────────────────────────────┐    │
│  │                    Storage Writer                                    │    │
│  │              Azure Table Storage Client                              │    │
│  └──────────────────────────────┬──────────────────────────────────────┘    │
│                                 │                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    Query API                                         │    │
│  │   GET /api/logs  GET /api/metrics  GET /api/applications            │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────┬───────────────────────────────────────────┘
                                  │
                                  │
          ┌───────────────────────┴───────────────────────┐
          │                                               │
          ▼                                               ▼
┌─────────────────────────┐                 ┌─────────────────────────┐
│   Azure Table Storage   │                 │     Frontend Dashboard  │
│  ┌───────────────────┐  │                 │  ┌───────────────────┐  │
│  │ LogsTable         │  │                 │  │ React + TypeScript│  │
│  │ MetricsTable      │  │                 │  │ • Log Viewer      │  │
│  │ TracesTable       │  │                 │  │ • Metrics Charts  │  │
│  │ ApplicationsTable │  │                 │  │ • App Dashboard   │  │
│  └───────────────────┘  │                 │  └───────────────────┘  │
└─────────────────────────┘                 └─────────────────────────┘
```

## Component Details

### 1. OTLP Receiver

Implements OpenTelemetry Protocol endpoints:

| Endpoint | Method | Content-Type | Description |
|----------|--------|--------------|-------------|
| `/v1/logs` | POST | application/json | Receive log records |
| `/v1/metrics` | POST | application/json | Receive metrics data |
| `/v1/traces` | POST | application/json | Receive trace spans |

### 2. Data Processing Pipeline

**Validation**
- Verify required fields (timestamp, resource attributes)
- Validate data types and formats
- Reject malformed requests with appropriate error codes

**Normalization**
- Convert timestamps to UTC
- Standardize attribute naming
- Map severity levels to consistent format

**Batching**
- Accumulate records for batch insert (configurable batch size)
- Flush on timeout or batch size threshold
- Handle back-pressure gracefully

**Enrichment**
- Add server-side timestamp
- Generate unique record IDs
- Add collector metadata

### 3. Storage Layer

Uses Azure Table Storage with the following design principles:
- PartitionKey: Application ID + Date (for time-range queries)
- RowKey: Timestamp + GUID (for uniqueness and ordering)
- Hot/Cold partitioning based on data age

### 4. Query API

RESTful API for frontend consumption:

| Endpoint | Description |
|----------|-------------|
| `GET /api/logs` | Query logs with filters |
| `GET /api/metrics` | Query metrics by name/time |
| `GET /api/traces` | Query trace spans |
| `GET /api/applications` | List registered applications |
| `GET /api/health` | Service health check |

### 5. Client SDKs

Each SDK provides:
- Automatic OpenTelemetry instrumentation
- Configurable batching and retry
- Low-overhead performance
- Easy integration (few lines of code)

## Security Considerations

- API Key authentication for SDK → Collector communication
- HTTPS only in production
- Input validation and sanitization
- Rate limiting per application

## Deployment Architecture

```
┌─────────────────────────────────────────┐
│           Azure Resource Group          │
│  ┌─────────────────────────────────┐    │
│  │  Azure App Service / Container  │    │
│  │  (APM Collector + Frontend)     │    │
│  └─────────────────────────────────┘    │
│                                         │
│  ┌─────────────────────────────────┐    │
│  │  Azure Storage Account          │    │
│  │  (Table Storage)                │    │
│  └─────────────────────────────────┘    │
└─────────────────────────────────────────┘
```

## Configuration

All configuration via environment variables or appsettings.json:

```json
{
  "AzureStorage": {
    "ConnectionString": "...",
    "LogsTableName": "Logs",
    "MetricsTableName": "Metrics",
    "TracesTableName": "Traces"
  },
  "Collector": {
    "BatchSize": 100,
    "FlushIntervalMs": 5000,
    "MaxQueueSize": 10000
  },
  "ApiKeys": {
    "Enabled": true,
    "Keys": ["key1", "key2"]
  }
}
```
