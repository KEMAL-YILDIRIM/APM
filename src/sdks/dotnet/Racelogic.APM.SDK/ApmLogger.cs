using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Racelogic.APM;

internal class ApmLogger : IApmLogger
{
    private readonly ApmOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentQueue<LogRecord> _queue = new();
    private readonly Timer _flushTimer;

    public ApmLogger(IOptions<ApmOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClient = httpClientFactory.CreateClient("ApmClient");

        _flushTimer = new Timer(
            async _ => await FlushAsync(),
            null,
            TimeSpan.FromMilliseconds(_options.FlushIntervalMs),
            TimeSpan.FromMilliseconds(_options.FlushIntervalMs));
    }

    public void LogTrace(string message, object? attributes = null)
        => Enqueue(1, "TRACE", message, null, attributes);

    public void LogDebug(string message, object? attributes = null)
        => Enqueue(5, "DEBUG", message, null, attributes);

    public void LogInfo(string message, object? attributes = null)
        => Enqueue(9, "INFO", message, null, attributes);

    public void LogWarn(string message, object? attributes = null)
        => Enqueue(13, "WARN", message, null, attributes);

    public void LogError(string message, Exception? exception = null, object? attributes = null)
        => Enqueue(17, "ERROR", message, exception, attributes);

    public void LogFatal(string message, Exception? exception = null, object? attributes = null)
        => Enqueue(21, "FATAL", message, exception, attributes);

    private void Enqueue(int severityNumber, string severityText, string message, Exception? exception, object? attributes)
    {
        var record = new LogRecord
        {
            TimeUnixNano = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000),
            SeverityNumber = severityNumber,
            SeverityText = severityText,
            Body = exception != null ? $"{message}\n{exception}" : message,
            Attributes = ConvertAttributes(attributes)
        };

        _queue.Enqueue(record);

        if (_queue.Count >= _options.BatchSize)
        {
            _ = FlushAsync();
        }
    }

    public async Task FlushAsync()
    {
        if (_queue.IsEmpty)
            return;

        var records = new List<LogRecord>();
        while (records.Count < _options.BatchSize && _queue.TryDequeue(out var record))
        {
            records.Add(record);
        }

        if (!records.Any())
            return;

        var request = new OtlpLogRequest
        {
            ResourceLogs = new List<ResourceLogs>
            {
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes = new List<KeyValue>
                        {
                            new() { Key = "service.name", Value = new AnyValue { StringValue = _options.ApplicationName } },
                            new() { Key = "service.version", Value = new AnyValue { StringValue = _options.ServiceVersion ?? "1.0.0" } },
                            new() { Key = "deployment.environment", Value = new AnyValue { StringValue = _options.Environment } }
                        }
                    },
                    ScopeLogs = new List<ScopeLogs>
                    {
                        new()
                        {
                            Scope = new InstrumentationScope { Name = "Racelogic.APM.SDK" },
                            LogRecords = records
                        }
                    }
                }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.Endpoint}/v1/logs")
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
            // Re-queue on failure (simple retry)
            foreach (var record in records)
            {
                _queue.Enqueue(record);
            }
        }
    }

    private static List<KeyValue>? ConvertAttributes(object? attributes)
    {
        if (attributes == null)
            return null;

        var result = new List<KeyValue>();
        var properties = attributes.GetType().GetProperties();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(attributes);
            if (value == null) continue;

            result.Add(new KeyValue
            {
                Key = prop.Name,
                Value = new AnyValue
                {
                    StringValue = value.ToString()
                }
            });
        }

        return result;
    }
}

// Internal OTLP models
internal class OtlpLogRequest
{
    public List<ResourceLogs> ResourceLogs { get; set; } = new();
}

internal class ResourceLogs
{
    public OtlpResource? Resource { get; set; }
    public List<ScopeLogs> ScopeLogs { get; set; } = new();
}

internal class ScopeLogs
{
    public InstrumentationScope? Scope { get; set; }
    public List<LogRecord> LogRecords { get; set; } = new();
}

internal class LogRecord
{
    public ulong TimeUnixNano { get; set; }
    public int SeverityNumber { get; set; }
    public string? SeverityText { get; set; }
    public string? Body { get; set; }
    public List<KeyValue>? Attributes { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
}

internal class OtlpResource
{
    public List<KeyValue>? Attributes { get; set; }
}

internal class InstrumentationScope
{
    public string? Name { get; set; }
    public string? Version { get; set; }
}

internal class KeyValue
{
    public string Key { get; set; } = string.Empty;
    public AnyValue? Value { get; set; }
}

internal class AnyValue
{
    public string? StringValue { get; set; }
    public long? IntValue { get; set; }
    public double? DoubleValue { get; set; }
    public bool? BoolValue { get; set; }
}
