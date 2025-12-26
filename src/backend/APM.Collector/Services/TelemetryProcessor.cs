using System.Collections.Concurrent;
using System.Text.Json;
using APM.Collector.Models.Entities;
using APM.Collector.Models.Otlp;

namespace APM.Collector.Services;

public class TelemetryProcessor : ITelemetryProcessor
{
    private readonly ConcurrentQueue<LogEntity> _logQueue = new();
    private readonly ConcurrentQueue<MetricEntity> _metricQueue = new();
    private readonly ConcurrentQueue<TraceEntity> _traceQueue = new();
    private readonly ILogger<TelemetryProcessor> _logger;

    public int LogQueueCount => _logQueue.Count;
    public int MetricQueueCount => _metricQueue.Count;
    public int TraceQueueCount => _traceQueue.Count;

    public TelemetryProcessor(ILogger<TelemetryProcessor> logger)
    {
        _logger = logger;
    }

    public void EnqueueLogs(OtlpLogRequest request, string applicationId)
    {
        foreach (var resourceLogs in request.ResourceLogs)
        {
            var resourceAttributes = ExtractAttributes(resourceLogs.Resource?.Attributes);
            var appName = GetAttributeValue(resourceLogs.Resource?.Attributes, "service.name") ?? applicationId;
            var serviceVersion = GetAttributeValue(resourceLogs.Resource?.Attributes, "service.version");
            var environment = GetAttributeValue(resourceLogs.Resource?.Attributes, "deployment.environment");

            foreach (var scopeLogs in resourceLogs.ScopeLogs)
            {
                foreach (var logRecord in scopeLogs.LogRecords)
                {
                    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(logRecord.TimeUnixNano / 1_000_000));

                    var entity = new LogEntity
                    {
                        PartitionKey = LogEntity.CreatePartitionKey(applicationId, timestamp),
                        RowKey = LogEntity.CreateRowKey(timestamp),
                        LogTimestamp = timestamp,
                        SeverityNumber = logRecord.SeverityNumber,
                        SeverityText = logRecord.SeverityText ?? GetSeverityText(logRecord.SeverityNumber),
                        Body = logRecord.Body?.ToString() ?? string.Empty,
                        TraceId = logRecord.TraceId,
                        SpanId = logRecord.SpanId,
                        ResourceAttributes = resourceAttributes,
                        LogAttributes = ExtractAttributes(logRecord.Attributes),
                        ApplicationId = applicationId,
                        ApplicationName = appName,
                        ServiceName = scopeLogs.Scope?.Name,
                        ServiceVersion = serviceVersion,
                        Environment = environment
                    };

                    _logQueue.Enqueue(entity);
                }
            }
        }

        _logger.LogDebug("Enqueued {Count} logs for application {AppId}",
            request.ResourceLogs.Sum(r => r.ScopeLogs.Sum(s => s.LogRecords.Count)), applicationId);
    }

    public void EnqueueMetrics(OtlpMetricRequest request, string applicationId)
    {
        foreach (var resourceMetrics in request.ResourceMetrics)
        {
            var resourceAttributes = ExtractAttributes(resourceMetrics.Resource?.Attributes);
            var appName = GetAttributeValue(resourceMetrics.Resource?.Attributes, "service.name") ?? applicationId;

            foreach (var scopeMetrics in resourceMetrics.ScopeMetrics)
            {
                foreach (var metric in scopeMetrics.Metrics)
                {
                    ProcessMetric(metric, applicationId, appName, resourceAttributes);
                }
            }
        }
    }

    private void ProcessMetric(Metric metric, string applicationId, string appName, string? resourceAttributes)
    {
        if (metric.Gauge != null)
        {
            foreach (var dp in metric.Gauge.DataPoints)
            {
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(dp.TimeUnixNano / 1_000_000));
                _metricQueue.Enqueue(CreateMetricEntity(metric, "Gauge", dp.GetValue(), timestamp,
                    applicationId, appName, resourceAttributes, dp.Attributes));
            }
        }

        if (metric.Sum != null)
        {
            foreach (var dp in metric.Sum.DataPoints)
            {
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(dp.TimeUnixNano / 1_000_000));
                _metricQueue.Enqueue(CreateMetricEntity(metric, "Counter", dp.GetValue(), timestamp,
                    applicationId, appName, resourceAttributes, dp.Attributes));
            }
        }

        if (metric.Histogram != null)
        {
            foreach (var dp in metric.Histogram.DataPoints)
            {
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(dp.TimeUnixNano / 1_000_000));
                var entity = CreateMetricEntity(metric, "Histogram", dp.Sum ?? 0, timestamp,
                    applicationId, appName, resourceAttributes, dp.Attributes);
                entity.ValueMin = dp.Min;
                entity.ValueMax = dp.Max;
                entity.ValueSum = dp.Sum;
                entity.ValueCount = (long)dp.Count;
                _metricQueue.Enqueue(entity);
            }
        }
    }

    private MetricEntity CreateMetricEntity(Metric metric, string type, double value, DateTimeOffset timestamp,
        string applicationId, string appName, string? resourceAttributes, List<KeyValue>? attributes)
    {
        return new MetricEntity
        {
            PartitionKey = MetricEntity.CreatePartitionKey(applicationId, timestamp),
            RowKey = MetricEntity.CreateRowKey(timestamp, metric.Name),
            MetricTimestamp = timestamp,
            MetricName = metric.Name,
            MetricDescription = metric.Description,
            MetricUnit = metric.Unit,
            MetricType = type,
            Value = value,
            Attributes = ExtractAttributes(attributes),
            ResourceAttributes = resourceAttributes,
            ApplicationId = applicationId,
            ApplicationName = appName
        };
    }

    public void EnqueueTraces(OtlpTraceRequest request, string applicationId)
    {
        foreach (var resourceSpans in request.ResourceSpans)
        {
            var resourceAttributes = ExtractAttributes(resourceSpans.Resource?.Attributes);
            var appName = GetAttributeValue(resourceSpans.Resource?.Attributes, "service.name") ?? applicationId;

            foreach (var scopeSpans in resourceSpans.ScopeSpans)
            {
                foreach (var span in scopeSpans.Spans)
                {
                    var startTime = DateTimeOffset.FromUnixTimeMilliseconds((long)(span.StartTimeUnixNano / 1_000_000));
                    var endTime = DateTimeOffset.FromUnixTimeMilliseconds((long)(span.EndTimeUnixNano / 1_000_000));

                    var entity = new TraceEntity
                    {
                        PartitionKey = TraceEntity.CreatePartitionKey(applicationId, startTime),
                        RowKey = TraceEntity.CreateRowKey(startTime, span.SpanId),
                        TraceId = span.TraceId,
                        SpanId = span.SpanId,
                        ParentSpanId = span.ParentSpanId,
                        SpanName = span.Name,
                        SpanKind = span.GetKindString(),
                        StartTime = startTime,
                        EndTime = endTime,
                        DurationMs = (long)(endTime - startTime).TotalMilliseconds,
                        StatusCode = span.Status?.GetCodeString() ?? "Unset",
                        StatusMessage = span.Status?.Message,
                        Attributes = ExtractAttributes(span.Attributes),
                        Events = span.Events != null ? JsonSerializer.Serialize(span.Events) : null,
                        ResourceAttributes = resourceAttributes,
                        ApplicationId = applicationId,
                        ApplicationName = appName
                    };

                    _traceQueue.Enqueue(entity);
                }
            }
        }
    }

    public bool TryDequeueLogs(int count, out List<LogEntity> entities)
    {
        entities = new List<LogEntity>();
        while (entities.Count < count && _logQueue.TryDequeue(out var entity))
        {
            entities.Add(entity);
        }
        return entities.Count > 0;
    }

    public bool TryDequeueMetrics(int count, out List<MetricEntity> entities)
    {
        entities = new List<MetricEntity>();
        while (entities.Count < count && _metricQueue.TryDequeue(out var entity))
        {
            entities.Add(entity);
        }
        return entities.Count > 0;
    }

    public bool TryDequeueTraces(int count, out List<TraceEntity> entities)
    {
        entities = new List<TraceEntity>();
        while (entities.Count < count && _traceQueue.TryDequeue(out var entity))
        {
            entities.Add(entity);
        }
        return entities.Count > 0;
    }

    private static string? ExtractAttributes(List<KeyValue>? attributes)
    {
        if (attributes == null || !attributes.Any())
            return null;

        var dict = attributes.ToDictionary(
            kv => kv.Key,
            kv => kv.Value?.GetValue());

        return JsonSerializer.Serialize(dict);
    }

    private static string? GetAttributeValue(List<KeyValue>? attributes, string key)
    {
        return attributes?.FirstOrDefault(a => a.Key == key)?.Value?.ToString();
    }

    private static string GetSeverityText(int severityNumber) => severityNumber switch
    {
        >= 21 => "FATAL",
        >= 17 => "ERROR",
        >= 13 => "WARN",
        >= 9 => "INFO",
        >= 5 => "DEBUG",
        _ => "TRACE"
    };
}
