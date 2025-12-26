using Racelogic.APM;

var builder = WebApplication.CreateBuilder(args);

// Add APM telemetry
builder.Services.AddApmTelemetry(options =>
{
    options.Endpoint = builder.Configuration["Apm:Endpoint"] ?? "http://localhost:5000";
    options.ApiKey = builder.Configuration["Apm:ApiKey"];
    options.ApplicationId = "sample-dotnet-api";
    options.ApplicationName = "Sample .NET Web API";
    options.Environment = builder.Environment.EnvironmentName;
    options.ServiceVersion = "1.0.0";
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Sample endpoints
app.MapGet("/", (IApmLogger logger) =>
{
    logger.LogInfo("Home page accessed");
    return new { message = "Welcome to Sample .NET Web API", timestamp = DateTime.UtcNow };
});

app.MapGet("/users", (IApmLogger logger, IApmMetrics metrics) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    logger.LogInfo("Fetching users list");

    // Simulate some work
    var users = Enumerable.Range(1, 10).Select(i => new
    {
        id = i,
        name = $"User {i}",
        email = $"user{i}@example.com"
    }).ToList();

    stopwatch.Stop();
    metrics.RecordHistogram("get_users_duration_ms", stopwatch.ElapsedMilliseconds);
    metrics.RecordGauge("users_count", users.Count);

    logger.LogDebug("Returned {Count} users", new { Count = users.Count });

    return users;
});

app.MapGet("/users/{id}", (int id, IApmLogger logger, IApmMetrics metrics) =>
{
    logger.LogInfo("Fetching user", new { UserId = id });
    metrics.RecordCounter("user_fetch_count", 1, new Dictionary<string, object> { ["user_id"] = id });

    if (id <= 0 || id > 100)
    {
        logger.LogWarn("User not found", new { UserId = id });
        return Results.NotFound(new { error = "User not found" });
    }

    return Results.Ok(new
    {
        id,
        name = $"User {id}",
        email = $"user{id}@example.com",
        createdAt = DateTime.UtcNow.AddDays(-id)
    });
});

app.MapPost("/users", (CreateUserRequest request, IApmLogger logger, IApmMetrics metrics) =>
{
    logger.LogInfo("Creating new user", new { Name = request.Name, Email = request.Email });
    metrics.RecordCounter("user_created_count", 1);

    var newUser = new
    {
        id = Random.Shared.Next(100, 1000),
        name = request.Name,
        email = request.Email,
        createdAt = DateTime.UtcNow
    };

    logger.LogInfo("User created successfully", new { UserId = newUser.id });

    return Results.Created($"/users/{newUser.id}", newUser);
});

app.MapGet("/error", (IApmLogger logger) =>
{
    logger.LogError("Intentional error for testing", new InvalidOperationException("This is a test error"));
    return Results.Problem("Something went wrong!");
});

app.MapGet("/slow", async (IApmLogger logger, IApmMetrics metrics) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    logger.LogInfo("Starting slow operation");

    // Simulate slow operation
    await Task.Delay(Random.Shared.Next(500, 2000));

    stopwatch.Stop();
    metrics.RecordHistogram("slow_operation_duration_ms", stopwatch.ElapsedMilliseconds);

    logger.LogInfo("Slow operation completed", new { DurationMs = stopwatch.ElapsedMilliseconds });

    return new { message = "Slow operation completed", durationMs = stopwatch.ElapsedMilliseconds };
});

app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

app.Run();

record CreateUserRequest(string Name, string Email);
