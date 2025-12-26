# Sample .NET Web API

This is a sample ASP.NET Core Web API demonstrating APM SDK integration.

## Quick Start

1. Make sure the APM Collector is running at `http://localhost:5000`

2. Build and run the application:
   ```bash
   dotnet run
   ```

3. The app will run at `http://localhost:5001` (or the port shown in console)

## Available Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | Welcome message |
| `/users` | GET | List all users |
| `/users/{id}` | GET | Get user by ID |
| `/users` | POST | Create new user |
| `/error` | GET | Trigger an error (for testing) |
| `/slow` | GET | Slow operation (500-2000ms) |
| `/health` | GET | Health check |

## Configuration

Edit `appsettings.json` to configure the APM endpoint:

```json
{
  "Apm": {
    "Endpoint": "http://localhost:5000",
    "ApiKey": null
  }
}
```

Or use environment variables:
- `Apm__Endpoint` - APM Collector URL
- `Apm__ApiKey` - API key for authentication

## Testing Telemetry

```bash
# Generate some traffic
curl http://localhost:5001/
curl http://localhost:5001/users
curl http://localhost:5001/users/1
curl -X POST http://localhost:5001/users -H "Content-Type: application/json" -d "{\"name\":\"Test\",\"email\":\"test@example.com\"}"
curl http://localhost:5001/error
curl http://localhost:5001/slow
```

Then check the APM Dashboard at `http://localhost:3000` to see the logs and metrics.

## SDK Integration Example

```csharp
// Program.cs
builder.Services.AddApmTelemetry(options =>
{
    options.Endpoint = "http://localhost:5000";
    options.ApplicationName = "MyApp";
});

// In your services/controllers
public class MyService
{
    private readonly IApmLogger _logger;
    private readonly IApmMetrics _metrics;

    public MyService(IApmLogger logger, IApmMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public void DoWork()
    {
        _logger.LogInfo("Starting work", new { UserId = 123 });
        _metrics.RecordCounter("work_started", 1);
    }
}
```
