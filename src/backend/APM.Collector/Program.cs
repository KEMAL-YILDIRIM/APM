using APM.Collector.Configuration;
using APM.Collector.Services;
using APM.Collector.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<AzureStorageOptions>(
    builder.Configuration.GetSection("AzureStorage"));
builder.Services.Configure<CollectorOptions>(
    builder.Configuration.GetSection("Collector"));

// Services
builder.Services.AddSingleton<ITableStorageService, TableStorageService>();
builder.Services.AddSingleton<ITelemetryProcessor, TelemetryProcessor>();
builder.Services.AddHostedService<BatchProcessorService>();

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "APM Collector API", Version = "v1" });
});

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMiddleware<ApiKeyAuthMiddleware>();
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

app.Run();
