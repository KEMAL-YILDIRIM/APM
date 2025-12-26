using Microsoft.AspNetCore.Mvc;
using APM.Collector.Services;

namespace APM.Collector.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly ITableStorageService _storageService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        ITableStorageService storageService,
        ILogger<MetricsController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Query metrics with optional filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMetrics(
        [FromQuery] string? applicationId = null,
        [FromQuery] string? name = null,
        [FromQuery] DateTimeOffset? startTime = null,
        [FromQuery] DateTimeOffset? endTime = null,
        [FromQuery] string? aggregation = null,
        [FromQuery] int limit = 1000)
    {
        startTime ??= DateTimeOffset.UtcNow.AddHours(-6);
        endTime ??= DateTimeOffset.UtcNow;

        var metrics = await _storageService.QueryMetricsAsync(
            applicationId,
            name,
            startTime,
            endTime,
            limit);

        var result = metrics.Select(m => new
        {
            id = m.RowKey,
            timestamp = m.MetricTimestamp,
            name = m.MetricName,
            description = m.MetricDescription,
            unit = m.MetricUnit,
            type = m.MetricType,
            value = m.Value,
            min = m.ValueMin,
            max = m.ValueMax,
            sum = m.ValueSum,
            count = m.ValueCount,
            applicationId = m.ApplicationId,
            applicationName = m.ApplicationName,
            attributes = ParseJson(m.Attributes)
        });

        // Apply aggregation if requested
        if (!string.IsNullOrEmpty(aggregation) && !string.IsNullOrEmpty(name))
        {
            var grouped = result.GroupBy(m => new
            {
                m.name,
                // Bucket by minute for aggregation
                bucket = new DateTimeOffset(
                    m.timestamp.Year, m.timestamp.Month, m.timestamp.Day,
                    m.timestamp.Hour, m.timestamp.Minute, 0,
                    m.timestamp.Offset)
            });

            var aggregated = grouped.Select(g => new
            {
                timestamp = g.Key.bucket,
                name = g.Key.name,
                value = aggregation.ToLowerInvariant() switch
                {
                    "sum" => g.Sum(x => x.value),
                    "avg" or "average" => g.Average(x => x.value),
                    "min" => g.Min(x => x.value),
                    "max" => g.Max(x => x.value),
                    "count" => (double)g.Count(),
                    _ => g.Average(x => x.value)
                }
            }).OrderBy(x => x.timestamp);

            return Ok(new
            {
                data = aggregated,
                count = aggregated.Count()
            });
        }

        return Ok(new
        {
            data = result,
            count = result.Count()
        });
    }

    /// <summary>
    /// Get list of available metric names
    /// </summary>
    [HttpGet("names")]
    public async Task<IActionResult> GetMetricNames([FromQuery] string? applicationId = null)
    {
        var metrics = await _storageService.QueryMetricsAsync(
            applicationId,
            null,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            10000);

        var names = metrics
            .Select(m => m.MetricName)
            .Distinct()
            .OrderBy(n => n);

        return Ok(new { names });
    }

    private static object? ParseJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<object>(json);
        }
        catch
        {
            return null;
        }
    }
}
