using Microsoft.AspNetCore.Mvc;
using APM.Collector.Models.Entities;
using APM.Collector.Services;
using System.Security.Cryptography;

namespace APM.Collector.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly ITableStorageService _storageService;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(
        ITableStorageService storageService,
        ILogger<ApplicationsController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Get all registered applications
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetApplications()
    {
        var apps = await _storageService.GetAllApplicationsAsync();

        var result = apps.Select(a => new
        {
            id = a.ApplicationId,
            name = a.ApplicationName,
            environment = a.Environment,
            createdAt = a.CreatedAt,
            lastSeenAt = a.LastSeenAt,
            isActive = a.IsActive,
            tags = ParseJson(a.Tags)
        });

        return Ok(new { data = result });
    }

    /// <summary>
    /// Get a single application by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetApplication(string id)
    {
        var app = await _storageService.GetApplicationAsync(id);

        if (app == null)
        {
            return NotFound(new { error = "Application not found" });
        }

        return Ok(new
        {
            id = app.ApplicationId,
            name = app.ApplicationName,
            environment = app.Environment,
            createdAt = app.CreatedAt,
            lastSeenAt = app.LastSeenAt,
            isActive = app.IsActive,
            tags = ParseJson(app.Tags)
        });
    }

    /// <summary>
    /// Register a new application
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequest request)
    {
        var applicationId = request.Id ?? Guid.NewGuid().ToString("N")[..12];
        var apiKey = GenerateApiKey();

        var entity = new ApplicationEntity
        {
            PartitionKey = "applications",
            RowKey = applicationId,
            ApplicationId = applicationId,
            ApplicationName = request.Name,
            ApiKeyHash = HashApiKey(apiKey),
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            Environment = request.Environment ?? "development",
            IsActive = true
        };

        await _storageService.UpsertApplicationAsync(entity);

        _logger.LogInformation("Created application {AppId} ({AppName})", applicationId, request.Name);

        return CreatedAtAction(nameof(GetApplication), new { id = applicationId }, new
        {
            id = applicationId,
            name = request.Name,
            apiKey = apiKey, // Only returned on creation
            environment = entity.Environment,
            createdAt = entity.CreatedAt
        });
    }

    /// <summary>
    /// Regenerate API key for an application
    /// </summary>
    [HttpPost("{id}/regenerate-key")]
    public async Task<IActionResult> RegenerateApiKey(string id)
    {
        var app = await _storageService.GetApplicationAsync(id);

        if (app == null)
        {
            return NotFound(new { error = "Application not found" });
        }

        var newApiKey = GenerateApiKey();
        app.ApiKeyHash = HashApiKey(newApiKey);

        await _storageService.UpsertApplicationAsync(app);

        _logger.LogInformation("Regenerated API key for application {AppId}", id);

        return Ok(new { apiKey = newApiKey });
    }

    private static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_")[..43];
    }

    private static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(apiKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
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

public record CreateApplicationRequest(
    string Name,
    string? Id = null,
    string? Environment = null
);
