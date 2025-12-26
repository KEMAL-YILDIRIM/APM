# APM - Application Performance Monitoring

## Project Overview

APM is a lightweight, self-hosted application performance monitoring solution that collects telemetry data (logs, metrics, and traces) from applications using OpenTelemetry standards and stores them in Azure Table Storage for cost-effective, scalable storage.

## Goals

1. **Collect Telemetry**: Gather logs, metrics, and traces from any application using OpenTelemetry
2. **Cost-Effective Storage**: Store telemetry data in Azure Table Storage
3. **Multi-Language SDKs**: Provide easy-to-use SDKs for .NET, JavaScript, and Python applications
4. **Visualization**: Web-based frontend to view, search, and analyze telemetry data

## Core Components

### 1. Collector Service (Backend)
- OTLP (OpenTelemetry Protocol) receiver endpoint
- Data transformation and normalization
- Azure Table Storage writer
- REST API for frontend queries

### 2. Client SDKs
- .NET SDK (NuGet package)
- JavaScript/TypeScript SDK (npm package)
- Python SDK (PyPI package)

### 3. Frontend Dashboard
- Real-time telemetry viewer
- Error log search and filtering
- Metrics visualization with charts
- Application health overview

## Technology Stack

| Component | Technology |
|-----------|------------|
| Backend API | .NET 8 / ASP.NET Core |
| Storage | Azure Table Storage |
| Frontend | React + TypeScript |
| Client SDKs | Native implementations per language |
| Protocol | OpenTelemetry Protocol (OTLP) over HTTP |

## Non-Goals (Out of Scope for v1.0)

- Real-time alerting and notifications
- Advanced APM features (distributed tracing visualization)
- Multi-tenant support
- Authentication/Authorization (will use Azure AD in future)
- Log aggregation and correlation

## Success Criteria

- Successfully receive and store telemetry from all three SDK types
- Frontend displays logs and metrics with <1s latency
- SDK integration requires <10 lines of code per application
- Storage cost remains predictable with Azure Table Storage pricing
