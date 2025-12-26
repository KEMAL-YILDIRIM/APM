using System.Text.Json.Serialization;

namespace APM.Collector.Models.Otlp;

/// <summary>
/// OTLP Traces export request (simplified JSON format).
/// </summary>
public class OtlpTraceRequest
{
    [JsonPropertyName("resourceSpans")]
    public List<ResourceSpans> ResourceSpans { get; set; } = new();
}

public class ResourceSpans
{
    [JsonPropertyName("resource")]
    public OtlpResource? Resource { get; set; }

    [JsonPropertyName("scopeSpans")]
    public List<ScopeSpans> ScopeSpans { get; set; } = new();
}

public class ScopeSpans
{
    [JsonPropertyName("scope")]
    public InstrumentationScope? Scope { get; set; }

    [JsonPropertyName("spans")]
    public List<Span> Spans { get; set; } = new();
}

public class Span
{
    [JsonPropertyName("traceId")]
    public string TraceId { get; set; } = string.Empty;

    [JsonPropertyName("spanId")]
    public string SpanId { get; set; } = string.Empty;

    [JsonPropertyName("parentSpanId")]
    public string? ParentSpanId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public int Kind { get; set; } // 0=Unspecified, 1=Internal, 2=Server, 3=Client, 4=Producer, 5=Consumer

    [JsonPropertyName("startTimeUnixNano")]
    public ulong StartTimeUnixNano { get; set; }

    [JsonPropertyName("endTimeUnixNano")]
    public ulong EndTimeUnixNano { get; set; }

    [JsonPropertyName("attributes")]
    public List<KeyValue>? Attributes { get; set; }

    [JsonPropertyName("events")]
    public List<SpanEvent>? Events { get; set; }

    [JsonPropertyName("status")]
    public SpanStatus? Status { get; set; }

    public string GetKindString() => Kind switch
    {
        1 => "Internal",
        2 => "Server",
        3 => "Client",
        4 => "Producer",
        5 => "Consumer",
        _ => "Unspecified"
    };
}

public class SpanEvent
{
    [JsonPropertyName("timeUnixNano")]
    public ulong TimeUnixNano { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public List<KeyValue>? Attributes { get; set; }
}

public class SpanStatus
{
    [JsonPropertyName("code")]
    public int Code { get; set; } // 0=Unset, 1=Ok, 2=Error

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    public string GetCodeString() => Code switch
    {
        1 => "Ok",
        2 => "Error",
        _ => "Unset"
    };
}
