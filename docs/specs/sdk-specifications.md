# APM SDK Specifications

## Overview

This document specifies the client SDKs for integrating applications with the APM Collector. Each SDK provides a consistent developer experience while following language-specific idioms.

## Common Requirements

All SDKs must:
1. Support OpenTelemetry semantic conventions
2. Provide automatic batching with configurable settings
3. Implement retry logic with exponential backoff
4. Be non-blocking / async by default
5. Have minimal external dependencies
6. Integrate with existing OpenTelemetry pipelines

---

## .NET SDK

### Package Info
- **Name**: `Racelogic.APM.SDK`
- **Target**: .NET 6.0+, .NET Standard 2.1
- **Dependencies**: `Azure.Data.Tables`, `OpenTelemetry`

### Installation
```bash
dotnet add package Racelogic.APM.SDK
```

### Basic Usage
```csharp
using Racelogic.APM;

// Configuration in Program.cs or Startup.cs
builder.Services.AddApmTelemetry(options =>
{
    options.Endpoint = "https://apm.example.com";
    options.ApiKey = "your-api-key";
    options.ApplicationName = "MyWebApp";
    options.Environment = "production";
});

// Usage via dependency injection
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

        try
        {
            // ... do work
            _metrics.RecordGauge("work_duration_ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError("Work failed", ex, new { UserId = 123 });
        }
    }
}
```

### API Surface

```csharp
namespace Racelogic.APM
{
    public interface IApmLogger
    {
        void LogTrace(string message, object? attributes = null);
        void LogDebug(string message, object? attributes = null);
        void LogInfo(string message, object? attributes = null);
        void LogWarn(string message, object? attributes = null);
        void LogError(string message, Exception? exception = null, object? attributes = null);
        void LogFatal(string message, Exception? exception = null, object? attributes = null);
    }

    public interface IApmMetrics
    {
        void RecordCounter(string name, long value, IDictionary<string, object>? attributes = null);
        void RecordGauge(string name, double value, IDictionary<string, object>? attributes = null);
        void RecordHistogram(string name, double value, IDictionary<string, object>? attributes = null);
    }

    public interface IApmTracer
    {
        ISpan StartSpan(string name, SpanKind kind = SpanKind.Internal);
    }

    public class ApmOptions
    {
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public string ApplicationName { get; set; }
        public string? ApplicationId { get; set; }
        public string Environment { get; set; } = "development";
        public int BatchSize { get; set; } = 100;
        public int FlushIntervalMs { get; set; } = 5000;
        public int MaxRetries { get; set; } = 3;
        public bool EnableAutoInstrumentation { get; set; } = true;
    }
}
```

### OpenTelemetry Integration
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddApmExporter(options => { ... }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddApmExporter(options => { ... }));
```

---

## JavaScript/TypeScript SDK

### Package Info
- **Name**: `@racelogic/apm-sdk`
- **Target**: Node.js 18+, Browser (ES2020+)
- **Dependencies**: `@opentelemetry/api`

### Installation
```bash
npm install @racelogic/apm-sdk
```

### Basic Usage (Node.js)
```typescript
import { ApmClient, ApmLogger, ApmMetrics } from '@racelogic/apm-sdk';

// Initialize once at app startup
const apm = ApmClient.init({
  endpoint: 'https://apm.example.com',
  apiKey: 'your-api-key',
  applicationName: 'my-node-app',
  environment: 'production',
});

// Get logger and metrics
const logger = apm.getLogger();
const metrics = apm.getMetrics();

// Usage
logger.info('User logged in', { userId: 123, method: 'oauth' });
metrics.counter('user_logins', 1, { method: 'oauth' });

// Express middleware (automatic request tracing)
import express from 'express';
const app = express();
app.use(apm.expressMiddleware());

// Graceful shutdown
process.on('SIGTERM', async () => {
  await apm.flush();
  await apm.shutdown();
});
```

### Browser Usage
```typescript
import { ApmClient } from '@racelogic/apm-sdk/browser';

const apm = ApmClient.init({
  endpoint: 'https://apm.example.com',
  apiKey: 'your-api-key',
  applicationName: 'my-spa',
  environment: 'production',
});

// Automatic error capturing
window.addEventListener('error', (event) => {
  apm.logger.error('Uncaught error', {
    message: event.message,
    filename: event.filename,
    lineno: event.lineno,
  });
});

// Manual logging
apm.logger.info('Page loaded', { route: window.location.pathname });
```

### API Surface
```typescript
interface ApmOptions {
  endpoint: string;
  apiKey: string;
  applicationName: string;
  applicationId?: string;
  environment?: string;
  batchSize?: number;
  flushIntervalMs?: number;
  maxRetries?: number;
}

interface ApmLogger {
  trace(message: string, attributes?: Record<string, unknown>): void;
  debug(message: string, attributes?: Record<string, unknown>): void;
  info(message: string, attributes?: Record<string, unknown>): void;
  warn(message: string, attributes?: Record<string, unknown>): void;
  error(message: string, error?: Error, attributes?: Record<string, unknown>): void;
  fatal(message: string, error?: Error, attributes?: Record<string, unknown>): void;
}

interface ApmMetrics {
  counter(name: string, value: number, attributes?: Record<string, unknown>): void;
  gauge(name: string, value: number, attributes?: Record<string, unknown>): void;
  histogram(name: string, value: number, attributes?: Record<string, unknown>): void;
}

interface ApmTracer {
  startSpan(name: string, options?: SpanOptions): Span;
  withSpan<T>(name: string, fn: (span: Span) => T | Promise<T>): Promise<T>;
}
```

---

## Python SDK

### Package Info
- **Name**: `racelogic-apm`
- **Target**: Python 3.9+
- **Dependencies**: `opentelemetry-api`, `httpx`

### Installation
```bash
pip install racelogic-apm
```

### Basic Usage
```python
from racelogic_apm import ApmClient

# Initialize once at app startup
apm = ApmClient(
    endpoint="https://apm.example.com",
    api_key="your-api-key",
    application_name="my-python-app",
    environment="production",
)

# Get logger and metrics
logger = apm.logger
metrics = apm.metrics

# Usage
logger.info("User logged in", user_id=123, method="oauth")
metrics.counter("user_logins", 1, method="oauth")

# Context manager for spans
with apm.tracer.start_span("process_order") as span:
    span.set_attribute("order_id", 456)
    # ... do work

# Decorator for automatic tracing
@apm.trace("calculate_shipping")
def calculate_shipping(order_id: int) -> float:
    # ... implementation
    return 9.99

# Flask integration
from flask import Flask
app = Flask(__name__)
apm.instrument_flask(app)

# Django integration
# In settings.py:
MIDDLEWARE = [
    'racelogic_apm.django.ApmMiddleware',
    # ... other middleware
]
APM_CONFIG = {
    'endpoint': 'https://apm.example.com',
    'api_key': 'your-api-key',
    'application_name': 'my-django-app',
}

# Graceful shutdown
import atexit
atexit.register(apm.shutdown)
```

### API Surface
```python
from dataclasses import dataclass
from typing import Optional, Dict, Any

@dataclass
class ApmConfig:
    endpoint: str
    api_key: str
    application_name: str
    application_id: Optional[str] = None
    environment: str = "development"
    batch_size: int = 100
    flush_interval_ms: int = 5000
    max_retries: int = 3

class ApmLogger:
    def trace(self, message: str, **attributes: Any) -> None: ...
    def debug(self, message: str, **attributes: Any) -> None: ...
    def info(self, message: str, **attributes: Any) -> None: ...
    def warn(self, message: str, **attributes: Any) -> None: ...
    def error(self, message: str, exception: Optional[Exception] = None, **attributes: Any) -> None: ...
    def fatal(self, message: str, exception: Optional[Exception] = None, **attributes: Any) -> None: ...

class ApmMetrics:
    def counter(self, name: str, value: int, **attributes: Any) -> None: ...
    def gauge(self, name: str, value: float, **attributes: Any) -> None: ...
    def histogram(self, name: str, value: float, **attributes: Any) -> None: ...

class ApmTracer:
    def start_span(self, name: str, kind: SpanKind = SpanKind.INTERNAL) -> Span: ...
    def trace(self, name: str) -> Callable: ...  # Decorator
```

---

## Error Handling

All SDKs implement consistent error handling:

1. **Network Errors**: Retry with exponential backoff (1s, 2s, 4s)
2. **Authentication Errors (401/403)**: Log error, do not retry
3. **Server Errors (5xx)**: Retry with backoff
4. **Validation Errors (400)**: Log error, do not retry
5. **Queue Full**: Drop oldest records, log warning

SDKs should never throw exceptions to the application for telemetry failures. All failures are handled internally with appropriate logging.

---

## Performance Requirements

| Metric | Target |
|--------|--------|
| Memory overhead | <10MB |
| CPU overhead | <1% |
| Network (batched) | 1 request per flush interval |
| Latency impact | <1ms per operation |
| Startup time | <100ms |
