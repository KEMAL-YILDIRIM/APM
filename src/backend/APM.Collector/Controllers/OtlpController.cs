using Microsoft.AspNetCore.Mvc;
using APM.Collector.Models.Otlp;
using APM.Collector.Services;

namespace APM.Collector.Controllers;

[ApiController]
[Route("v1")]
public class OtlpController : ControllerBase
{
    private readonly ITelemetryProcessor _telemetryProcessor;
    private readonly ILogger<OtlpController> _logger;

    public OtlpController(
        ITelemetryProcessor telemetryProcessor,
        ILogger<OtlpController> logger)
    {
        _telemetryProcessor = telemetryProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Receive OTLP log records
    /// </summary>
    [HttpPost("logs")]
    [Consumes("application/json")]
    public IActionResult ReceiveLogs([FromBody] OtlpLogRequest request)
    {
        var applicationId = GetApplicationId();

        _logger.LogDebug("Received logs from application {AppId}", applicationId);
        _telemetryProcessor.EnqueueLogs(request, applicationId);

        return Ok(new { partialSuccess = new { } });
    }

    /// <summary>
    /// Receive OTLP metrics
    /// </summary>
    [HttpPost("metrics")]
    [Consumes("application/json")]
    public IActionResult ReceiveMetrics([FromBody] OtlpMetricRequest request)
    {
        var applicationId = GetApplicationId();

        _logger.LogDebug("Received metrics from application {AppId}", applicationId);
        _telemetryProcessor.EnqueueMetrics(request, applicationId);

        return Ok(new { partialSuccess = new { } });
    }

    /// <summary>
    /// Receive OTLP trace spans
    /// </summary>
    [HttpPost("traces")]
    [Consumes("application/json")]
    public IActionResult ReceiveTraces([FromBody] OtlpTraceRequest request)
    {
        var applicationId = GetApplicationId();

        _logger.LogDebug("Received traces from application {AppId}", applicationId);
        _telemetryProcessor.EnqueueTraces(request, applicationId);

        return Ok(new { partialSuccess = new { } });
    }

    private string GetApplicationId()
    {
        return HttpContext.Items["ApplicationId"]?.ToString() ?? "default";
    }
}
