using Microsoft.AspNetCore.Mvc;
using APM.Collector.Services;

namespace APM.Collector.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ITableStorageService _storageService;
    private readonly ILogger<LogsController> _logger;

    public LogsController(
        ITableStorageService storageService,
        ILogger<LogsController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Query logs with optional filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? applicationId = null,
        [FromQuery] DateTimeOffset? startTime = null,
        [FromQuery] DateTimeOffset? endTime = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? search = null,
        [FromQuery] int limit = 100)
    {
        startTime ??= DateTimeOffset.UtcNow.AddHours(-1);
        endTime ??= DateTimeOffset.UtcNow;

        int? minSeverity = severity?.ToUpperInvariant() switch
        {
            "TRACE" => 1,
            "DEBUG" => 5,
            "INFO" => 9,
            "WARN" or "WARNING" => 13,
            "ERROR" => 17,
            "FATAL" => 21,
            _ => null
        };

        var logs = await _storageService.QueryLogsAsync(
            applicationId,
            startTime,
            endTime,
            minSeverity,
            search,
            limit);

        var result = logs.Select(l => new
        {
            id = l.RowKey,
            timestamp = l.LogTimestamp,
            severity = l.SeverityText,
            severityNumber = l.SeverityNumber,
            message = l.Body,
            applicationId = l.ApplicationId,
            applicationName = l.ApplicationName,
            serviceName = l.ServiceName,
            traceId = l.TraceId,
            spanId = l.SpanId,
            attributes = ParseJson(l.LogAttributes),
            resourceAttributes = ParseJson(l.ResourceAttributes)
        });

        return Ok(new
        {
            data = result,
            count = result.Count()
        });
    }

    /// <summary>
    /// Get a single log entry by ID
    /// </summary>
    [HttpGet("{partitionKey}/{rowKey}")]
    public async Task<IActionResult> GetLog(string partitionKey, string rowKey)
    {
        // For now, return not implemented - would need exact partition key
        return NotFound();
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
