# APM - Application Performance Monitoring

## Project Overview

A lightweight, self-hosted application performance monitoring solution that collects telemetry data (logs, metrics, traces) using OpenTelemetry standards and stores them in Azure Table Storage. Built by Racelogic.

## Tech Stack

### Backend (APM.Collector)
- **.NET 8** - ASP.NET Core Web API
- **Azure.Data.Tables** - Azure Table Storage SDK
- **Swashbuckle** - Swagger/OpenAPI documentation
- **Nullable enabled**, **Implicit usings enabled**

### Frontend (apm-dashboard)
- **React 18** with TypeScript
- **Vite 5** - Build tool
- **TanStack Query v5** - Data fetching & caching
- **TanStack Table v8** - Table component
- **React Router v6** - Routing
- **Recharts** - Charting library
- **Tailwind CSS** - Styling
- **Axios** - HTTP client
- **date-fns** - Date utilities

### SDKs
- **dotnet** - `Racelogic.APM.SDK` (.NET SDK)
- **javascript** - `@racelogic/apm-sdk` (JS/TS SDK)
- **python** - `racelogic-apm` (Python SDK)

## Project Structure

```
APM/
├── docs/
│   ├── architecture/           # Architecture diagrams
│   ├── specs/                  # Specifications
│   │   ├── architecture.md
│   │   ├── data-model.md
│   │   ├── frontend-specifications.md
│   │   ├── project-overview.md
│   │   └── sdk-specifications.md
│   └── IMPLEMENTATION-PLAN.md
├── src/
│   ├── backend/
│   │   └── APM.Collector/      # .NET 8 backend
│   │       ├── Configuration/  # Options classes
│   │       ├── Controllers/    # API controllers
│   │       ├── Middleware/     # Custom middleware
│   │       ├── Models/
│   │       │   ├── Entities/   # Table storage entities
│   │       │   └── Otlp/       # OTLP request models
│   │       ├── Services/       # Business logic
│   │       └── Program.cs      # Entry point
│   ├── frontend/               # React dashboard
│   │   └── src/
│   │       ├── api/            # API client
│   │       ├── components/     # React components
│   │       └── pages/          # Page components
│   └── sdks/
│       ├── dotnet/             # .NET SDK
│       ├── javascript/         # JS/TS SDK
│       └── python/             # Python SDK
├── samples/                    # Sample applications
└── tests/                      # Test projects
```

## Key Backend Components

### Controllers
- `OtlpController` - OTLP endpoints (`/v1/logs`, `/v1/metrics`, `/v1/traces`)
- `LogsController` - Query API for logs (`/api/logs`)
- `MetricsController` - Query API for metrics (`/api/metrics`)
- `ApplicationsController` - Application management (`/api/applications`)

### Services
- `TableStorageService` - Azure Table Storage operations
- `TelemetryProcessor` - Processes incoming OTLP data
- `BatchProcessorService` - Background service for batch processing

### Middleware
- `ApiKeyAuthMiddleware` - API key authentication

### Configuration
- `AzureStorageOptions` - Azure Storage connection settings
- `CollectorOptions` - Collector behavior (batch size, flush interval)

## API Endpoints

### OTLP Endpoints (Ingestion)
- `POST /v1/logs` - Receive logs
- `POST /v1/metrics` - Receive metrics
- `POST /v1/traces` - Receive traces

### Query API
- `GET /api/logs` - Query logs
- `GET /api/metrics` - Query metrics
- `GET /api/applications` - List applications

### Health
- `GET /health` - Health check

## Development Commands

### Backend
```bash
cd src/backend/APM.Collector
dotnet run                    # Start the collector (port 5000)
dotnet build                  # Build
dotnet test                   # Run tests
```

### Frontend
```bash
cd src/frontend
npm install                   # Install dependencies
npm run dev                   # Start dev server (port 3000)
npm run build                 # Production build
npm run lint                  # Run ESLint
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
- `AzureStorage__ConnectionString` - Azure Storage connection string
- `VITE_API_URL` - Frontend API URL (dev only)

## Prerequisites
- .NET 8 SDK
- Node.js 18+
- Python 3.9+
- Azurite (Azure Storage Emulator) or Azure Storage Account

## Code Conventions

### C# (.NET)
- Use nullable reference types
- Follow .NET naming conventions (PascalCase for public, camelCase for private)
- Use dependency injection
- Configure services in `Program.cs`
- Use Options pattern for configuration

### TypeScript/React
- Functional components with hooks
- TanStack Query for server state
- Tailwind CSS for styling (no inline styles)
- TypeScript strict mode

### General
- Keep files focused and small
- Use async/await for I/O operations
- Add proper error handling at boundaries

---

## SDK Coding Guidelines

All SDKs follow OpenTelemetry semantic conventions and provide a consistent developer experience while respecting language-specific idioms.

### Common SDK Requirements
1. Support OpenTelemetry semantic conventions
2. Provide automatic batching with configurable settings
3. Implement retry logic with exponential backoff (1s, 2s, 4s)
4. Be non-blocking / async by default
5. Have minimal external dependencies
6. Never throw exceptions for telemetry failures - handle internally with logging
7. Support graceful shutdown with `flush()` and `shutdown()` methods

### Performance Targets
| Metric | Target |
|--------|--------|
| Memory overhead | <10MB |
| CPU overhead | <1% |
| Network (batched) | 1 request per flush interval |
| Latency impact | <1ms per operation |
| Startup time | <100ms |

---

### .NET SDK (`Racelogic.APM.SDK`)

**Location**: `src/sdks/dotnet/Racelogic.APM.SDK/`

**Target Frameworks**: .NET 6.0, .NET 8.0 (multi-target)

**Namespace**: `Racelogic.APM`

**Key Files**:
- `IApmLogger.cs` / `ApmLogger.cs` - Logging interface and implementation
- `IApmMetrics.cs` / `ApmMetrics.cs` - Metrics interface and implementation
- `ApmOptions.cs` - Configuration options
- `ServiceCollectionExtensions.cs` - DI registration

**Coding Conventions**:
- Use interfaces (`IApmLogger`, `IApmMetrics`) for testability
- Use `IServiceCollection` extension methods for DI registration
- Use Options pattern (`IOptions<ApmOptions>`)
- Use `IHttpClientFactory` for HTTP clients
- XML documentation comments on all public APIs
- File-scoped namespaces
- Nullable reference types enabled

**API Pattern**:
```csharp
// Registration
builder.Services.AddApmTelemetry(options => {
    options.Endpoint = "...";
    options.ApplicationName = "...";
});

// Usage via DI
public class MyService(IApmLogger logger, IApmMetrics metrics)
{
    public void DoWork()
    {
        logger.LogInfo("message", new { key = value });
        metrics.RecordCounter("name", 1);
    }
}
```

**Log Methods**: `LogTrace`, `LogDebug`, `LogInfo`, `LogWarn`, `LogError`, `LogFatal`

**Metrics Methods**: `RecordCounter`, `RecordGauge`, `RecordHistogram`

---

### JavaScript/TypeScript SDK (`@racelogic/apm-sdk`)

**Location**: `src/sdks/javascript/`

**Target**: Node.js 18+, Browser (ES2020+)

**Build Tool**: tsup

**Test Framework**: vitest

**Key Files**:
- `src/index.ts` - Main entry, `ApmClient` class
- `src/logger.ts` - `ApmLogger` class
- `src/metrics.ts` - `ApmMetrics` class
- `src/types.ts` - TypeScript interfaces

**Coding Conventions**:
- TypeScript strict mode
- Use classes with singleton pattern (`ApmClient.init()`, `ApmClient.getInstance()`)
- Export types separately (`export type { ... }`)
- Use JSDoc comments for documentation
- camelCase for methods and properties
- Async methods return `Promise<void>`
- Support both CommonJS and ESM (`dist/index.js`, `dist/index.mjs`)

**API Pattern**:
```typescript
// Initialization (singleton)
const apm = ApmClient.init({
  endpoint: '...',
  applicationName: '...',
});

// Usage
apm.logger.info('message', { key: 'value' });
apm.metrics.counter('name', 1, { tag: 'value' });

// Express middleware
app.use(apm.expressMiddleware());

// Cleanup
await apm.flush();
await apm.shutdown();
```

**Log Methods**: `trace`, `debug`, `info`, `warn`, `error`, `fatal`

**Metrics Methods**: `counter`, `gauge`, `histogram`

**Type Definitions**:
```typescript
interface ApmOptions {
  endpoint: string;
  applicationName: string;
  apiKey?: string;
  environment?: string;
  batchSize?: number;
  flushIntervalMs?: number;
}

interface LogAttributes {
  [key: string]: string | number | boolean | undefined;
}
```

---

### Python SDK (`racelogic-apm`)

**Location**: `src/sdks/python/`

**Target**: Python 3.9+

**Package Manager**: hatch (pyproject.toml)

**HTTP Client**: httpx

**Test Framework**: pytest, pytest-asyncio

**Key Files**:
- `src/racelogic_apm/__init__.py` - Package exports
- `src/racelogic_apm/client.py` - `ApmClient` class
- `src/racelogic_apm/config.py` - `ApmConfig` dataclass
- `src/racelogic_apm/logger.py` - `ApmLogger` class
- `src/racelogic_apm/metrics.py` - `ApmMetrics` class

**Coding Conventions**:
- Use type hints on all functions
- Use `@dataclass` for configuration
- Use `@property` for getters
- Use snake_case for methods and variables
- Use `**kwargs` for flexible attributes
- Register `atexit` handler for automatic cleanup
- Singleton via class variable `_instance`
- Docstrings on all public methods (Google style)

**API Pattern**:
```python
# Initialization
apm = ApmClient(
    endpoint="...",
    application_name="...",
)

# Usage
apm.logger.info("message", user_id=123, action="login")
apm.metrics.counter("name", 1, tag="value")

# Decorator for tracing
@apm.trace("operation_name")
def my_function():
    ...

# Flask integration
apm.instrument_flask(app)

# Cleanup (automatic via atexit, or manual)
apm.shutdown()
```

**Log Methods**: `trace`, `debug`, `info`, `warn`, `error`, `fatal`

**Metrics Methods**: `counter`, `gauge`, `histogram`

---

### SDK Development Commands

**.NET SDK**:
```bash
cd src/sdks/dotnet/Racelogic.APM.SDK
dotnet build
dotnet test
dotnet pack                    # Create NuGet package
```

**JavaScript SDK**:
```bash
cd src/sdks/javascript
npm install
npm run build                  # Build with tsup
npm run dev                    # Watch mode
npm test                       # Run vitest
```

**Python SDK**:
```bash
cd src/sdks/python
pip install -e ".[dev]"        # Install in dev mode
pytest                         # Run tests
hatch build                    # Build package
```

---

### Error Handling (All SDKs)

| Error Type | Action |
|------------|--------|
| Network errors | Retry with exponential backoff |
| Auth errors (401/403) | Log error, do not retry |
| Server errors (5xx) | Retry with backoff |
| Validation errors (400) | Log error, do not retry |
| Queue full | Drop oldest records, log warning |
