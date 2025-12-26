using APM.Collector.Models.Entities;

namespace APM.Collector.Services;

public interface ITableStorageService
{
    Task InitializeAsync();

    // Logs
    Task InsertLogAsync(LogEntity entity);
    Task InsertLogsBatchAsync(IEnumerable<LogEntity> entities);
    Task<IEnumerable<LogEntity>> QueryLogsAsync(
        string? applicationId = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        int? minSeverity = null,
        string? searchText = null,
        int maxResults = 100,
        string? continuationToken = null);

    // Metrics
    Task InsertMetricAsync(MetricEntity entity);
    Task InsertMetricsBatchAsync(IEnumerable<MetricEntity> entities);
    Task<IEnumerable<MetricEntity>> QueryMetricsAsync(
        string? applicationId = null,
        string? metricName = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        int maxResults = 1000);

    // Traces
    Task InsertTraceAsync(TraceEntity entity);
    Task InsertTracesBatchAsync(IEnumerable<TraceEntity> entities);
    Task<IEnumerable<TraceEntity>> QueryTracesAsync(
        string? applicationId = null,
        string? traceId = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        int maxResults = 100);

    // Applications
    Task<ApplicationEntity?> GetApplicationAsync(string applicationId);
    Task<IEnumerable<ApplicationEntity>> GetAllApplicationsAsync();
    Task UpsertApplicationAsync(ApplicationEntity entity);
    Task UpdateApplicationLastSeenAsync(string applicationId);
}
