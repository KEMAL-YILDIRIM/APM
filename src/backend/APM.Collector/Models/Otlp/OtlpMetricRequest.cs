using System.Text.Json.Serialization;

namespace APM.Collector.Models.Otlp;

/// <summary>
/// OTLP Metrics export request (simplified JSON format).
/// </summary>
public class OtlpMetricRequest
{
    [JsonPropertyName("resourceMetrics")]
    public List<ResourceMetrics> ResourceMetrics { get; set; } = new();
}

public class ResourceMetrics
{
    [JsonPropertyName("resource")]
    public OtlpResource? Resource { get; set; }

    [JsonPropertyName("scopeMetrics")]
    public List<ScopeMetrics> ScopeMetrics { get; set; } = new();
}

public class ScopeMetrics
{
    [JsonPropertyName("scope")]
    public InstrumentationScope? Scope { get; set; }

    [JsonPropertyName("metrics")]
    public List<Metric> Metrics { get; set; } = new();
}

public class Metric
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("gauge")]
    public Gauge? Gauge { get; set; }

    [JsonPropertyName("sum")]
    public Sum? Sum { get; set; }

    [JsonPropertyName("histogram")]
    public Histogram? Histogram { get; set; }
}

public class Gauge
{
    [JsonPropertyName("dataPoints")]
    public List<NumberDataPoint> DataPoints { get; set; } = new();
}

public class Sum
{
    [JsonPropertyName("dataPoints")]
    public List<NumberDataPoint> DataPoints { get; set; } = new();

    [JsonPropertyName("aggregationTemporality")]
    public int AggregationTemporality { get; set; }

    [JsonPropertyName("isMonotonic")]
    public bool IsMonotonic { get; set; }
}

public class Histogram
{
    [JsonPropertyName("dataPoints")]
    public List<HistogramDataPoint> DataPoints { get; set; } = new();

    [JsonPropertyName("aggregationTemporality")]
    public int AggregationTemporality { get; set; }
}

public class NumberDataPoint
{
    [JsonPropertyName("timeUnixNano")]
    public ulong TimeUnixNano { get; set; }

    [JsonPropertyName("startTimeUnixNano")]
    public ulong? StartTimeUnixNano { get; set; }

    [JsonPropertyName("asDouble")]
    public double? AsDouble { get; set; }

    [JsonPropertyName("asInt")]
    public long? AsInt { get; set; }

    [JsonPropertyName("attributes")]
    public List<KeyValue>? Attributes { get; set; }

    public double GetValue() => AsDouble ?? AsInt ?? 0;
}

public class HistogramDataPoint
{
    [JsonPropertyName("timeUnixNano")]
    public ulong TimeUnixNano { get; set; }

    [JsonPropertyName("startTimeUnixNano")]
    public ulong? StartTimeUnixNano { get; set; }

    [JsonPropertyName("count")]
    public ulong Count { get; set; }

    [JsonPropertyName("sum")]
    public double? Sum { get; set; }

    [JsonPropertyName("min")]
    public double? Min { get; set; }

    [JsonPropertyName("max")]
    public double? Max { get; set; }

    [JsonPropertyName("bucketCounts")]
    public List<ulong>? BucketCounts { get; set; }

    [JsonPropertyName("explicitBounds")]
    public List<double>? ExplicitBounds { get; set; }

    [JsonPropertyName("attributes")]
    public List<KeyValue>? Attributes { get; set; }
}
