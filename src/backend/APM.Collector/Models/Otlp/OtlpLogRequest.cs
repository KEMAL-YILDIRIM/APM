using System.Text.Json.Serialization;

namespace APM.Collector.Models.Otlp;

/// <summary>
/// OTLP Logs export request (simplified JSON format).
/// Based on OpenTelemetry Protocol specification.
/// </summary>
public class OtlpLogRequest
{
    [JsonPropertyName("resourceLogs")]
    public List<ResourceLogs> ResourceLogs { get; set; } = new();
}

public class ResourceLogs
{
    [JsonPropertyName("resource")]
    public OtlpResource? Resource { get; set; }

    [JsonPropertyName("scopeLogs")]
    public List<ScopeLogs> ScopeLogs { get; set; } = new();
}

public class ScopeLogs
{
    [JsonPropertyName("scope")]
    public InstrumentationScope? Scope { get; set; }

    [JsonPropertyName("logRecords")]
    public List<LogRecord> LogRecords { get; set; } = new();
}

public class LogRecord
{
    [JsonPropertyName("timeUnixNano")]
    public ulong TimeUnixNano { get; set; }

    [JsonPropertyName("observedTimeUnixNano")]
    public ulong? ObservedTimeUnixNano { get; set; }

    [JsonPropertyName("severityNumber")]
    public int SeverityNumber { get; set; }

    [JsonPropertyName("severityText")]
    public string? SeverityText { get; set; }

    [JsonPropertyName("body")]
    public OtlpAnyValue? Body { get; set; }

    [JsonPropertyName("attributes")]
    public List<KeyValue>? Attributes { get; set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    [JsonPropertyName("spanId")]
    public string? SpanId { get; set; }
}

public class OtlpResource
{
    [JsonPropertyName("attributes")]
    public List<KeyValue>? Attributes { get; set; }
}

public class InstrumentationScope
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

public class KeyValue
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public OtlpAnyValue? Value { get; set; }
}

public class OtlpAnyValue
{
    [JsonPropertyName("stringValue")]
    public string? StringValue { get; set; }

    [JsonPropertyName("intValue")]
    public long? IntValue { get; set; }

    [JsonPropertyName("doubleValue")]
    public double? DoubleValue { get; set; }

    [JsonPropertyName("boolValue")]
    public bool? BoolValue { get; set; }

    [JsonPropertyName("arrayValue")]
    public ArrayValue? ArrayValue { get; set; }

    [JsonPropertyName("kvlistValue")]
    public KvlistValue? KvlistValue { get; set; }

    public object? GetValue()
    {
        if (StringValue != null) return StringValue;
        if (IntValue != null) return IntValue;
        if (DoubleValue != null) return DoubleValue;
        if (BoolValue != null) return BoolValue;
        return null;
    }

    public override string ToString()
    {
        return GetValue()?.ToString() ?? string.Empty;
    }
}

public class ArrayValue
{
    [JsonPropertyName("values")]
    public List<OtlpAnyValue>? Values { get; set; }
}

public class KvlistValue
{
    [JsonPropertyName("values")]
    public List<KeyValue>? Values { get; set; }
}
