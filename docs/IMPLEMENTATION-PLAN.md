# APM Implementation Plan

## Phase 1: Foundation (Backend Core)

### 1.1 Project Setup
- [ ] Create .NET 8 solution structure
- [ ] Set up project references
- [ ] Configure appsettings.json
- [ ] Add Azure.Data.Tables NuGet package
- [ ] Set up logging and configuration

### 1.2 Azure Table Storage Layer
- [ ] Create table entity models (LogEntity, MetricEntity, TraceEntity)
- [ ] Implement repository pattern for table operations
- [ ] Create partition key / row key generators
- [ ] Implement batch insert operations
- [ ] Add connection string configuration

### 1.3 OTLP Receiver
- [ ] Create OTLP DTOs matching OpenTelemetry protobuf schema
- [ ] Implement POST /v1/logs endpoint
- [ ] Implement POST /v1/metrics endpoint
- [ ] Implement POST /v1/traces endpoint
- [ ] Add request validation
- [ ] Add API key authentication middleware

### 1.4 Data Processing Pipeline
- [ ] Create background service for processing queue
- [ ] Implement batching logic
- [ ] Add data normalization
- [ ] Implement retry logic with dead-letter handling

---

## Phase 2: Query API

### 2.1 Log Query API
- [ ] GET /api/logs with filters (time, app, severity, search)
- [ ] GET /api/logs/{id} for single log
- [ ] Implement pagination with continuation tokens
- [ ] Add query optimization for common patterns

### 2.2 Metrics Query API
- [ ] GET /api/metrics with aggregation support
- [ ] GET /api/metrics/names for available metrics
- [ ] Implement time-series data formatting
- [ ] Add grouping and bucketing

### 2.3 Application Management API
- [ ] GET /api/applications
- [ ] POST /api/applications
- [ ] GET /api/applications/{id}
- [ ] POST /api/applications/{id}/regenerate-key

### 2.4 Health & Status
- [ ] GET /api/health
- [ ] GET /api/status (queue depth, etc.)

---

## Phase 3: Client SDKs

### 3.1 .NET SDK
- [ ] Create class library project
- [ ] Implement IApmLogger interface
- [ ] Implement IApmMetrics interface
- [ ] Create HTTP client for OTLP transmission
- [ ] Add batching and retry logic
- [ ] Create DI extension methods
- [ ] Write unit tests
- [ ] Create NuGet package spec

### 3.2 JavaScript SDK
- [ ] Initialize npm package
- [ ] Implement ApmClient class
- [ ] Create logger and metrics modules
- [ ] Add Node.js and browser support
- [ ] Implement batching with web workers
- [ ] Add TypeScript definitions
- [ ] Write unit tests
- [ ] Configure npm publishing

### 3.3 Python SDK
- [ ] Initialize Python package structure
- [ ] Implement ApmClient class
- [ ] Create logger and metrics modules
- [ ] Add async support with httpx
- [ ] Implement batching with background thread
- [ ] Add type hints (PEP 484)
- [ ] Write unit tests
- [ ] Configure PyPI publishing

---

## Phase 4: Frontend Dashboard

### 4.1 Project Setup
- [ ] Create Vite + React + TypeScript project
- [ ] Configure Tailwind CSS
- [ ] Set up TanStack Query
- [ ] Create API client layer
- [ ] Set up routing (React Router)

### 4.2 Layout & Navigation
- [ ] Create main layout with sidebar
- [ ] Implement header with navigation
- [ ] Add responsive menu
- [ ] Create breadcrumb component

### 4.3 Dashboard Page
- [ ] Application cards component
- [ ] Error trend chart
- [ ] Recent errors table
- [ ] Health status indicators

### 4.4 Logs Page
- [ ] Time range selector component
- [ ] Filter bar (app, severity, search)
- [ ] Log list with virtual scrolling
- [ ] Log detail panel
- [ ] Live tail toggle

### 4.5 Metrics Page
- [ ] Metric selector
- [ ] Time series chart component
- [ ] Aggregation controls
- [ ] Export functionality

### 4.6 Applications Page
- [ ] Application list
- [ ] Application detail view
- [ ] API key management

---

## Phase 5: Integration & Testing

### 5.1 Sample Applications
- [ ] .NET Web API sample
- [ ] Node.js Express sample
- [ ] Python Flask sample

### 5.2 Integration Tests
- [ ] End-to-end test suite
- [ ] SDK integration tests
- [ ] Load testing

### 5.3 Documentation
- [ ] API documentation (OpenAPI/Swagger)
- [ ] SDK quickstart guides
- [ ] Deployment guide

---

## Dependencies & Order

```
Phase 1.1 → Phase 1.2 → Phase 1.3 → Phase 1.4
                            ↓
                       Phase 2.x
                            ↓
                    ┌───────┴───────┐
                    ↓               ↓
               Phase 3.x       Phase 4.x
                    ↓               ↓
                    └───────┬───────┘
                            ↓
                       Phase 5.x
```

---

## Estimated Component Sizes

| Component | Estimated Files | Complexity |
|-----------|-----------------|------------|
| Backend API | 30-40 files | High |
| .NET SDK | 15-20 files | Medium |
| JS SDK | 15-20 files | Medium |
| Python SDK | 10-15 files | Medium |
| Frontend | 40-50 files | High |
| Tests | 20-30 files | Medium |

---

## Risk Factors

1. **Azure Table Storage Query Limitations**
   - Mitigation: Design partition keys for common query patterns

2. **OTLP Protocol Compatibility**
   - Mitigation: Start with JSON format, add protobuf later

3. **High-Volume Ingestion**
   - Mitigation: Implement backpressure and batching

4. **Frontend Performance with Large Datasets**
   - Mitigation: Virtual scrolling, pagination, server-side filtering
