# APM - Application Performance Monitoring

A lightweight, self-hosted application performance monitoring solution that collects telemetry data using OpenTelemetry standards and stores them in Azure Table Storage.

## Features

- **OpenTelemetry Compatible**: OTLP receiver for logs, metrics, and traces
- **Cost-Effective Storage**: Uses Azure Table Storage
- **Multi-Language SDKs**: .NET, JavaScript/TypeScript, and Python
- **Modern Dashboard**: React-based frontend for viewing telemetry

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Client Applications                          │
│   [.NET App]        [JS/Node App]        [Python App]           │
│   APM.SDK           @racelogic/apm-sdk   racelogic-apm          │
└───────────────────────────┬─────────────────────────────────────┘
                            │ OTLP/HTTP
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                     APM Collector Service                        │
│   • OTLP Endpoints (/v1/logs, /v1/metrics, /v1/traces)         │
│   • Data Processing & Batching                                   │
│   • Query API (/api/logs, /api/metrics, /api/applications)      │
└───────────────────────────┬─────────────────────────────────────┘
                            │
              ┌─────────────┴─────────────┐
              ▼                           ▼
┌─────────────────────────┐   ┌─────────────────────────┐
│   Azure Table Storage   │   │    Frontend Dashboard   │
│   • Logs                │   │    • React + TypeScript │
│   • Metrics             │   │    • TanStack Query     │
│   • Traces              │   │    • Recharts           │
│   • Applications        │   │    • Tailwind CSS       │
└─────────────────────────┘   └─────────────────────────┘
```

## Quick Start

### 1. Start the Collector

```bash
cd src/backend/APM.Collector
dotnet run
```

The collector will start at `http://localhost:5000`.

### 2. Start the Frontend

```bash
cd src/frontend
npm install
npm run dev
```

The dashboard will be available at `http://localhost:3000`.

### 3. Integrate Your Application

#### .NET

```csharp
// Program.cs
builder.Services.AddApmTelemetry(options =>
{
    options.Endpoint = "http://localhost:5000";
    options.ApplicationName = "MyApp";
});

// Usage
public class MyService
{
    private readonly IApmLogger _logger;

    public void DoWork()
    {
        _logger.LogInfo("Processing started", new { UserId = 123 });
    }
}
```

#### JavaScript/Node.js

```javascript
import { ApmClient } from '@racelogic/apm-sdk';

const apm = ApmClient.init({
  endpoint: 'http://localhost:5000',
  applicationName: 'my-node-app',
});

apm.logger.info('User logged in', { userId: 123 });
apm.metrics.counter('user_logins', 1);
```

#### Python

```python
from racelogic_apm import ApmClient

apm = ApmClient(
    endpoint="http://localhost:5000",
    application_name="my-python-app",
)

apm.logger.info("User logged in", user_id=123)
apm.metrics.counter("user_logins", 1)
```

## Project Structure

```
APM/
├── docs/
│   ├── specs/                    # Specification documents
│   │   ├── project-overview.md
│   │   ├── architecture.md
│   │   ├── data-model.md
│   │   ├── sdk-specifications.md
│   │   └── frontend-specifications.md
│   └── IMPLEMENTATION-PLAN.md
├── src/
│   ├── backend/
│   │   └── APM.Collector/        # .NET 8 backend
│   ├── frontend/                  # React frontend
│   └── sdks/
│       ├── dotnet/               # .NET SDK
│       ├── javascript/           # JS/TS SDK
│       └── python/               # Python SDK
└── tests/
```

## Configuration

### Backend (appsettings.json)

```json
{
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "LogsTableName": "Logs",
    "MetricsTableName": "Metrics"
  },
  "Collector": {
    "BatchSize": 100,
    "FlushIntervalMs": 5000
  }
}
```

### Environment Variables

| Variable | Description |
|----------|-------------|
| `AzureStorage__ConnectionString` | Azure Storage connection string |
| `VITE_API_URL` | API URL for frontend (dev only) |

## Development

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- Python 3.9+
- Azure Storage Emulator (Azurite) or Azure Storage Account

### Running Tests

```bash
# Backend
cd src/backend/APM.Collector
dotnet test

# Frontend
cd src/frontend
npm test

# Python SDK
cd src/sdks/python
pytest
```

## Deployment

### Azure App Service

1. Create an Azure Storage Account
2. Create an Azure App Service (Linux, .NET 8)
3. Configure connection strings
4. Deploy using GitHub Actions or Azure DevOps

## License

MIT
