using Microsoft.Extensions.Options;
using APM.Collector.Configuration;

namespace APM.Collector.Services;

public class BatchProcessorService : BackgroundService
{
    private readonly TelemetryProcessor _telemetryProcessor;
    private readonly ITableStorageService _storageService;
    private readonly CollectorOptions _options;
    private readonly ILogger<BatchProcessorService> _logger;

    public BatchProcessorService(
        ITelemetryProcessor telemetryProcessor,
        ITableStorageService storageService,
        IOptions<CollectorOptions> options,
        ILogger<BatchProcessorService> logger)
    {
        _telemetryProcessor = (TelemetryProcessor)telemetryProcessor;
        _storageService = storageService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Batch processor service started. BatchSize={BatchSize}, FlushInterval={FlushInterval}ms",
            _options.BatchSize, _options.FlushIntervalMs);

        await _storageService.InitializeAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry batches");
            }

            await Task.Delay(_options.FlushIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessBatchesAsync()
    {
        // Process logs
        if (_telemetryProcessor.TryDequeueLogs(_options.BatchSize, out var logs) && logs.Any())
        {
            _logger.LogDebug("Processing {Count} logs", logs.Count);
            await _storageService.InsertLogsBatchAsync(logs);
        }

        // Process metrics
        if (_telemetryProcessor.TryDequeueMetrics(_options.BatchSize, out var metrics) && metrics.Any())
        {
            _logger.LogDebug("Processing {Count} metrics", metrics.Count);
            await _storageService.InsertMetricsBatchAsync(metrics);
        }

        // Process traces
        if (_telemetryProcessor.TryDequeueTraces(_options.BatchSize, out var traces) && traces.Any())
        {
            _logger.LogDebug("Processing {Count} traces", traces.Count);
            await _storageService.InsertTracesBatchAsync(traces);
        }
    }
}
