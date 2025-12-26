using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Racelogic.APM;

internal class ApmMetrics : IApmMetrics
{
    private readonly ApmOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentQueue<MetricData> _queue = new();
    private readonly Timer _flushTimer;

    public ApmMetrics(IOptions<ApmOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClient = httpClientFactory.CreateClient("ApmClient");

        _flushTimer = new Timer(
            async _ => await FlushAsync(),
            null,
            TimeSpan.FromMilliseconds(_options.FlushIntervalMs),
            TimeSpan.FromMilliseconds(_options.FlushIntervalMs));
    }

    public void RecordCounter(string name, long value, IDictionary<string, object>? attributes = null)
    {
        _queue.Enqueue(new MetricData
        {
            Name = name,
            Type = MetricType.Counter,
            Value = value,
            Timestamp = DateTimeOffset.UtcNow,
            Attributes = attributes
        });

        if (_queue.Count >= _options.BatchSize)
        {
            _ = FlushAsync();
        }
    }

    public void RecordGauge(string name, double value, IDictionary<string, object>? attributes = null)
    {
        _queue.Enqueue(new MetricData
        {
            Name = name,
            Type = MetricType.Gauge,
            Value = value,
            Timestamp = DateTimeOffset.UtcNow,
            Attributes = attributes
        });

        if (_queue.Count >= _options.BatchSize)
        {
            _ = FlushAsync();
        }
    }

    public void RecordHistogram(string name, double value, IDictionary<string, object>? attributes = null)
    {
        _queue.Enqueue(new MetricData
        {
            Name = name,
            Type = MetricType.Histogram,
            Value = value,
            Timestamp = DateTimeOffset.UtcNow,
            Attributes = attributes
        });

        if (_queue.Count >= _options.BatchSize)
        {
            _ = FlushAsync();
        }
    }

    public async Task FlushAsync()
    {
        if (_queue.IsEmpty)
            return;

        var metrics = new List<MetricData>();
        while (metrics.Count < _options.BatchSize && _queue.TryDequeue(out var metric))
        {
            metrics.Add(metric);
        }

        if (!metrics.Any())
            return;

        var groupedMetrics = metrics.GroupBy(m => m.Name);
        var otlpMetrics = new List<OtlpMetric>();

        foreach (var group in groupedMetrics)
        {
            var first = group.First();
            var otlpMetric = new OtlpMetric
            {
                Name = group.Key,
                Unit = "1"
            };

            var dataPoints = group.Select(m => new NumberDataPoint
            {
                TimeUnixNano = (ulong)(m.Timestamp.ToUnixTimeMilliseconds() * 1_000_000),
                AsDouble = m.Value,
                Attributes = ConvertAttributes(m.Attributes)
            }).ToList();

            switch (first.Type)
            {
                case MetricType.Gauge:
                    otlpMetric.Gauge = new OtlpGauge { DataPoints = dataPoints };
                    break;
                case MetricType.Counter:
                    otlpMetric.Sum = new OtlpSum
                    {
                        DataPoints = dataPoints,
                        IsMonotonic = true,
                        AggregationTemporality = 2
                    };
                    break;
                case MetricType.Histogram:
                    otlpMetric.Gauge = new OtlpGauge { DataPoints = dataPoints }; // Simplified
                    break;
            }

            otlpMetrics.Add(otlpMetric);
        }

        var request = new OtlpMetricRequest
        {
            ResourceMetrics = new List<ResourceMetrics>
            {
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes = new List<KeyValue>
                        {
                            new() { Key = "service.name", Value = new AnyValue { StringValue = _options.ApplicationName } }
                        }
                    },
                    ScopeMetrics = new List<ScopeMetrics>
                    {
                        new()
                        {
                            Scope = new InstrumentationScope { Name = "Racelogic.APM.SDK" },
                            Metrics = otlpMetrics
                        }
                    }
                }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.Endpoint}/v1/metrics")
            {
                Content = content
            };

            if (!string.IsNullOrEmpty(_options.ApiKey))
            {
                httpRequest.Headers.Add("X-API-Key", _options.ApiKey);
            }

            if (!string.IsNullOrEmpty(_options.ApplicationId))
            {
                httpRequest.Headers.Add("X-Application-Id", _options.ApplicationId);
            }

            await _httpClient.SendAsync(httpRequest);
        }
        catch
        {
            // Re-queue on failure
            foreach (var metric in metrics)
            {
                _queue.Enqueue(metric);
            }
        }
    }

    private static List<KeyValue>? ConvertAttributes(IDictionary<string, object>? attributes)
    {
        if (attributes == null || !attributes.Any())
            return null;

        return attributes.Select(kv => new KeyValue
        {
            Key = kv.Key,
            Value = new AnyValue { StringValue = kv.Value?.ToString() }
        }).ToList();
    }
}

internal enum MetricType
{
    Gauge,
    Counter,
    Histogram
}

internal class MetricData
{
    public string Name { get; set; } = string.Empty;
    public MetricType Type { get; set; }
    public double Value { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public IDictionary<string, object>? Attributes { get; set; }
}

// Internal OTLP models for metrics
internal class OtlpMetricRequest
{
    public List<ResourceMetrics> ResourceMetrics { get; set; } = new();
}

internal class ResourceMetrics
{
    public OtlpResource? Resource { get; set; }
    public List<ScopeMetrics> ScopeMetrics { get; set; } = new();
}

internal class ScopeMetrics
{
    public InstrumentationScope? Scope { get; set; }
    public List<OtlpMetric> Metrics { get; set; } = new();
}

internal class OtlpMetric
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public OtlpGauge? Gauge { get; set; }
    public OtlpSum? Sum { get; set; }
}

internal class OtlpGauge
{
    public List<NumberDataPoint> DataPoints { get; set; } = new();
}

internal class OtlpSum
{
    public List<NumberDataPoint> DataPoints { get; set; } = new();
    public int AggregationTemporality { get; set; }
    public bool IsMonotonic { get; set; }
}

internal class NumberDataPoint
{
    public ulong TimeUnixNano { get; set; }
    public double? AsDouble { get; set; }
    public List<KeyValue>? Attributes { get; set; }
}
