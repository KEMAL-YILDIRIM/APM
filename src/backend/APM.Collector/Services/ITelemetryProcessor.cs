using APM.Collector.Models.Otlp;

namespace APM.Collector.Services;

public interface ITelemetryProcessor
{
    void EnqueueLogs(OtlpLogRequest request, string applicationId);
    void EnqueueMetrics(OtlpMetricRequest request, string applicationId);
    void EnqueueTraces(OtlpTraceRequest request, string applicationId);

    int LogQueueCount { get; }
    int MetricQueueCount { get; }
    int TraceQueueCount { get; }
}
