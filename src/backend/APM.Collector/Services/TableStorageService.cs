using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using APM.Collector.Configuration;
using APM.Collector.Models.Entities;

namespace APM.Collector.Services;

public class TableStorageService : ITableStorageService
{
    private readonly TableServiceClient _serviceClient;
    private readonly AzureStorageOptions _options;
    private readonly ILogger<TableStorageService> _logger;

    private TableClient? _logsTable;
    private TableClient? _metricsTable;
    private TableClient? _tracesTable;
    private TableClient? _applicationsTable;

    public TableStorageService(
        IOptions<AzureStorageOptions> options,
        ILogger<TableStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _serviceClient = new TableServiceClient(_options.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing Azure Table Storage tables...");

        _logsTable = _serviceClient.GetTableClient(_options.LogsTableName);
        _metricsTable = _serviceClient.GetTableClient(_options.MetricsTableName);
        _tracesTable = _serviceClient.GetTableClient(_options.TracesTableName);
        _applicationsTable = _serviceClient.GetTableClient(_options.ApplicationsTableName);

        await _logsTable.CreateIfNotExistsAsync();
        await _metricsTable.CreateIfNotExistsAsync();
        await _tracesTable.CreateIfNotExistsAsync();
        await _applicationsTable.CreateIfNotExistsAsync();

        _logger.LogInformation("Azure Table Storage tables initialized");
    }

    #region Logs

    public async Task InsertLogAsync(LogEntity entity)
    {
        await EnsureInitializedAsync();
        await _logsTable!.AddEntityAsync(entity);
    }

    public async Task InsertLogsBatchAsync(IEnumerable<LogEntity> entities)
    {
        await EnsureInitializedAsync();

        var batches = entities
            .GroupBy(e => e.PartitionKey)
            .SelectMany(g => g.Chunk(100)); // Azure Table batch limit

        foreach (var batch in batches)
        {
            var transactions = batch
                .Select(e => new TableTransactionAction(TableTransactionActionType.Add, e))
                .ToList();

            if (transactions.Any())
            {
                await _logsTable!.SubmitTransactionAsync(transactions);
            }
        }
    }

    public async Task<IEnumerable<LogEntity>> QueryLogsAsync(
        string? applicationId = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        int? minSeverity = null,
        string? searchText = null,
        int maxResults = 100,
        string? continuationToken = null)
    {
        await EnsureInitializedAsync();

        var filters = new List<string>();

        if (!string.IsNullOrEmpty(applicationId) && startTime.HasValue)
        {
            var partitionKey = LogEntity.CreatePartitionKey(applicationId, startTime.Value);
            filters.Add($"PartitionKey eq '{partitionKey}'");
        }

        if (minSeverity.HasValue)
        {
            filters.Add($"SeverityNumber ge {minSeverity.Value}");
        }

        var filter = filters.Any() ? string.Join(" and ", filters) : null;

        var results = new List<LogEntity>();
        var query = _logsTable!.QueryAsync<LogEntity>(filter, maxResults);

        await foreach (var entity in query)
        {
            if (searchText != null && !entity.Body.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(entity);

            if (results.Count >= maxResults)
            {
                break;
            }
        }

        return results;
    }

    #endregion

    #region Metrics

    public async Task InsertMetricAsync(MetricEntity entity)
    {
        await EnsureInitializedAsync();
        await _metricsTable!.AddEntityAsync(entity);
    }

    public async Task InsertMetricsBatchAsync(IEnumerable<MetricEntity> entities)
    {
        await EnsureInitializedAsync();

        var batches = entities
            .GroupBy(e => e.PartitionKey)
            .SelectMany(g => g.Chunk(100));

        foreach (var batch in batches)
        {
            var transactions = batch
                .Select(e => new TableTransactionAction(TableTransactionActionType.Add, e))
                .ToList();

            if (transactions.Any())
            {
                await _metricsTable!.SubmitTransactionAsync(transactions);
            }
        }
    }

    public async Task<IEnumerable<MetricEntity>> QueryMetricsAsync(
        string? applicationId = null,
        string? metricName = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        int maxResults = 1000)
    {
        await EnsureInitializedAsync();

        var filters = new List<string>();

        if (!string.IsNullOrEmpty(applicationId) && startTime.HasValue)
        {
            var partitionKey = MetricEntity.CreatePartitionKey(applicationId, startTime.Value);
            filters.Add($"PartitionKey eq '{partitionKey}'");
        }

        if (!string.IsNullOrEmpty(metricName))
        {
            filters.Add($"MetricName eq '{metricName}'");
        }

        var filter = filters.Any() ? string.Join(" and ", filters) : null;

        var results = new List<MetricEntity>();
        var query = _metricsTable!.QueryAsync<MetricEntity>(filter, maxResults);

        await foreach (var entity in query)
        {
            results.Add(entity);
            if (results.Count >= maxResults) break;
        }

        return results;
    }

    #endregion

    #region Traces

    public async Task InsertTraceAsync(TraceEntity entity)
    {
        await EnsureInitializedAsync();
        await _tracesTable!.AddEntityAsync(entity);
    }

    public async Task InsertTracesBatchAsync(IEnumerable<TraceEntity> entities)
    {
        await EnsureInitializedAsync();

        var batches = entities
            .GroupBy(e => e.PartitionKey)
            .SelectMany(g => g.Chunk(100));

        foreach (var batch in batches)
        {
            var transactions = batch
                .Select(e => new TableTransactionAction(TableTransactionActionType.Add, e))
                .ToList();

            if (transactions.Any())
            {
                await _tracesTable!.SubmitTransactionAsync(transactions);
            }
        }
    }

    public async Task<IEnumerable<TraceEntity>> QueryTracesAsync(
        string? applicationId = null,
        string? traceId = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        int maxResults = 100)
    {
        await EnsureInitializedAsync();

        var filters = new List<string>();

        if (!string.IsNullOrEmpty(traceId))
        {
            filters.Add($"TraceId eq '{traceId}'");
        }

        var filter = filters.Any() ? string.Join(" and ", filters) : null;

        var results = new List<TraceEntity>();
        var query = _tracesTable!.QueryAsync<TraceEntity>(filter, maxResults);

        await foreach (var entity in query)
        {
            results.Add(entity);
            if (results.Count >= maxResults) break;
        }

        return results;
    }

    #endregion

    #region Applications

    public async Task<ApplicationEntity?> GetApplicationAsync(string applicationId)
    {
        await EnsureInitializedAsync();
        try
        {
            var response = await _applicationsTable!.GetEntityAsync<ApplicationEntity>("applications", applicationId);
            return response.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IEnumerable<ApplicationEntity>> GetAllApplicationsAsync()
    {
        await EnsureInitializedAsync();

        var results = new List<ApplicationEntity>();
        var query = _applicationsTable!.QueryAsync<ApplicationEntity>("PartitionKey eq 'applications'");

        await foreach (var entity in query)
        {
            results.Add(entity);
        }

        return results;
    }

    public async Task UpsertApplicationAsync(ApplicationEntity entity)
    {
        await EnsureInitializedAsync();
        await _applicationsTable!.UpsertEntityAsync(entity, TableUpdateMode.Replace);
    }

    public async Task UpdateApplicationLastSeenAsync(string applicationId)
    {
        var app = await GetApplicationAsync(applicationId);
        if (app != null)
        {
            app.LastSeenAt = DateTimeOffset.UtcNow;
            await UpsertApplicationAsync(app);
        }
    }

    #endregion

    private async Task EnsureInitializedAsync()
    {
        if (_logsTable == null)
        {
            await InitializeAsync();
        }
    }
}
