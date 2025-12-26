namespace APM.Collector.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ApplicationIdHeaderName = "X-Application-Id";

    public ApiKeyAuthMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth for health check and swagger
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/health") ||
            path.StartsWith("/swagger") ||
            path.StartsWith("/api/")) // Query API doesn't require auth for now
        {
            await _next(context);
            return;
        }

        // Check if API key auth is enabled
        var authEnabled = _configuration.GetValue<bool>("ApiKeys:Enabled");
        if (!authEnabled)
        {
            // If auth is disabled, still require application ID
            if (!context.Request.Headers.TryGetValue(ApplicationIdHeaderName, out var appIdValue))
            {
                // Generate a default application ID if not provided
                context.Items["ApplicationId"] = "default";
            }
            else
            {
                context.Items["ApplicationId"] = appIdValue.ToString();
            }

            await _next(context);
            return;
        }

        // Validate API key
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValue))
        {
            _logger.LogWarning("API key missing from request to {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API key is required" });
            return;
        }

        var apiKey = apiKeyValue.ToString();
        var validKeys = _configuration.GetSection("ApiKeys:Keys").Get<string[]>() ?? Array.Empty<string>();

        if (!validKeys.Contains(apiKey))
        {
            _logger.LogWarning("Invalid API key used for request to {Path}", path);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        // Get application ID
        if (!context.Request.Headers.TryGetValue(ApplicationIdHeaderName, out var appId))
        {
            context.Items["ApplicationId"] = "default";
        }
        else
        {
            context.Items["ApplicationId"] = appId.ToString();
        }

        await _next(context);
    }
}
