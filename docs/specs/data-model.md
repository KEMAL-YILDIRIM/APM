# APM Data Model Specification

## Overview

This document defines the data model for storing telemetry data in Azure Table Storage, following OpenTelemetry semantic conventions.

## Azure Table Storage Design

### Partition Key Strategy

To optimize for common query patterns (recent data, per-application filtering):

```
PartitionKey = "{ApplicationId}_{YYYY-MM-DD}"
```

This allows:
- Efficient queries by application
- Time-based partitioning for data lifecycle management
- Parallel queries across date ranges

### Row Key Strategy

```
RowKey = "{InvertedTimestamp}_{GUID}"
```

Where InvertedTimestamp = `DateTime.MaxValue.Ticks - Timestamp.Ticks`

This ensures:
- Most recent records first (default sort order)
- Uniqueness within partition
- Efficient range queries

---

## Table Schemas

### 1. Logs Table

| Property | Type | Description |
|----------|------|-------------|
| PartitionKey | string | `{AppId}_{Date}` |
| RowKey | string | `{InvertedTicks}_{GUID}` |
| Timestamp | DateTimeOffset | Azure auto-generated |
| LogTimestamp | DateTimeOffset | Original log timestamp |
| SeverityNumber | int | 1-24 (OTEL severity) |
| SeverityText | string | TRACE, DEBUG, INFO, WARN, ERROR, FATAL |
| Body | string | Log message body |
| TraceId | string | Trace correlation ID (optional) |
| SpanId | string | Span correlation ID (optional) |
| ResourceAttributes | string | JSON: resource info |
| LogAttributes | string | JSON: additional attributes |
| ApplicationId | string | Application identifier |
| ApplicationName | string | Human-readable app name |
| ServiceName | string | Service/component name |
| ServiceVersion | string | Application version |
| Environment | string | dev, staging, prod |

**OTEL Severity Mapping:**
| Number | Text |
|--------|------|
| 1-4 | TRACE |
| 5-8 | DEBUG |
| 9-12 | INFO |
| 13-16 | WARN |
| 17-20 | ERROR |
| 21-24 | FATAL |

### 2. Metrics Table

| Property | Type | Description |
|----------|------|-------------|
| PartitionKey | string | `{AppId}_{Date}` |
| RowKey | string | `{InvertedTicks}_{MetricName}_{GUID}` |
| Timestamp | DateTimeOffset | Azure auto-generated |
| MetricTimestamp | DateTimeOffset | Original metric timestamp |
| MetricName | string | Metric identifier |
| MetricDescription | string | Human-readable description |
| MetricUnit | string | Unit of measurement |
| MetricType | string | Gauge, Counter, Histogram |
| Value | double | Metric value |
| ValueMin | double? | Min (for histograms) |
| ValueMax | double? | Max (for histograms) |
| ValueSum | double? | Sum (for histograms) |
| ValueCount | long? | Count (for histograms) |
| Attributes | string | JSON: metric attributes |
| ResourceAttributes | string | JSON: resource info |
| ApplicationId | string | Application identifier |
| ApplicationName | string | Human-readable app name |

### 3. Traces Table

| Property | Type | Description |
|----------|------|-------------|
| PartitionKey | string | `{AppId}_{Date}` |
| RowKey | string | `{InvertedTicks}_{SpanId}` |
| Timestamp | DateTimeOffset | Azure auto-generated |
| TraceId | string | Trace identifier |
| SpanId | string | Span identifier |
| ParentSpanId | string | Parent span (optional) |
| SpanName | string | Operation name |
| SpanKind | string | Client, Server, Internal, Producer, Consumer |
| StartTime | DateTimeOffset | Span start time |
| EndTime | DateTimeOffset | Span end time |
| DurationMs | long | Duration in milliseconds |
| StatusCode | string | Unset, Ok, Error |
| StatusMessage | string | Error message if status=Error |
| Attributes | string | JSON: span attributes |
| Events | string | JSON: span events |
| ResourceAttributes | string | JSON: resource info |
| ApplicationId | string | Application identifier |
| ApplicationName | string | Human-readable app name |

### 4. Applications Table

| Property | Type | Description |
|----------|------|-------------|
| PartitionKey | string | "applications" |
| RowKey | string | ApplicationId |
| ApplicationId | string | Unique identifier |
| ApplicationName | string | Human-readable name |
| ApiKey | string | Hashed API key |
| CreatedAt | DateTimeOffset | Registration timestamp |
| LastSeenAt | DateTimeOffset | Last telemetry received |
| Environment | string | Primary environment |
| Tags | string | JSON: custom tags |
| IsActive | bool | Active status |

---

## Data Transfer Objects (DTOs)

### OTLP Log Record (Inbound)

```csharp
public class OtlpLogRecord
{
    public ulong TimeUnixNano { get; set; }
    public int SeverityNumber { get; set; }
    public string SeverityText { get; set; }
    public OtlpAnyValue Body { get; set; }
    public Dictionary<string, object> Attributes { get; set; }
    public string TraceId { get; set; }
    public string SpanId { get; set; }
}
```

### Log Query Response (Outbound)

```csharp
public class LogEntry
{
    public string Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Severity { get; set; }
    public string Message { get; set; }
    public string ApplicationName { get; set; }
    public string ServiceName { get; set; }
    public string TraceId { get; set; }
    public Dictionary<string, object> Attributes { get; set; }
}
```

---

## Query Patterns

### Common Queries

1. **Recent logs by application**
   ```
   PartitionKey eq '{AppId}_{Today}'
   ```

2. **Logs by severity (errors only)**
   ```
   PartitionKey eq '{AppId}_{Date}' and SeverityNumber ge 17
   ```

3. **Logs in time range**
   ```
   PartitionKey eq '{AppId}_{Date}'
   and RowKey ge '{InvertedEndTime}'
   and RowKey le '{InvertedStartTime}'
   ```

4. **Logs by trace ID** (cross-partition query)
   ```
   TraceId eq '{traceId}'
   ```

---

## Data Retention

| Tier | Age | Action |
|------|-----|--------|
| Hot | 0-7 days | Full access, quick queries |
| Warm | 8-30 days | Available, slower queries |
| Cold | 31-90 days | Archived, on-demand access |
| Delete | >90 days | Automatic deletion |

Retention policies enforced via Azure Storage lifecycle management or scheduled cleanup job.

---

## Size Limits

| Limit | Value |
|-------|-------|
| PartitionKey + RowKey | 1KB max |
| Single property | 64KB max |
| Entity total | 1MB max |
| Body/Message field | 32KB recommended |
| Attributes JSON | 64KB max |
| Batch insert | 100 entities max |
