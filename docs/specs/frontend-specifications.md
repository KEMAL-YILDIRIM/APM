# APM Frontend Specifications

## Overview

The APM Frontend Dashboard provides a web-based interface for viewing, searching, and analyzing telemetry data collected by the APM system.

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | React 18+ |
| Language | TypeScript 5+ |
| Build Tool | Vite |
| Styling | Tailwind CSS |
| State Management | TanStack Query (React Query) |
| Charts | Recharts or Chart.js |
| Table | TanStack Table |
| Date/Time | date-fns |
| HTTP Client | Axios or Fetch |

---

## Page Structure

### 1. Dashboard (Home)
Overview of all monitored applications with key metrics.

**Features:**
- Application cards showing health status
- Error count (last 24h)
- Request count (last 24h)
- Quick links to logs/metrics per app
- System health indicator

**Layout:**
```
┌────────────────────────────────────────────────────────────────┐
│  APM Dashboard                              [🔔] [⚙️] [User]  │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  Applications Overview                                         │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐           │
│  │ WebAPI       │ │ Frontend     │ │ Worker       │           │
│  │ ✅ Healthy   │ │ ⚠️ Warning   │ │ ✅ Healthy   │           │
│  │ Errors: 3    │ │ Errors: 47   │ │ Errors: 0    │           │
│  │ Req: 12.5k   │ │ Req: 89.2k   │ │ Jobs: 1.2k   │           │
│  └──────────────┘ └──────────────┘ └──────────────┘           │
│                                                                │
│  Error Trend (24h)                                             │
│  ┌────────────────────────────────────────────────────────┐   │
│  │  📊 Chart showing error count over time                │   │
│  └────────────────────────────────────────────────────────┘   │
│                                                                │
│  Recent Errors                                                 │
│  ┌────────────────────────────────────────────────────────┐   │
│  │  [Table: Time | App | Message | Count ]                │   │
│  └────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────┘
```

### 2. Logs Page
Full log search and exploration interface.

**Features:**
- Time range selector (presets + custom)
- Application filter (multi-select)
- Severity filter (multi-select)
- Full-text search
- Live tail mode (auto-refresh)
- Log detail panel
- Copy log as JSON
- Link to related traces

**Layout:**
```
┌────────────────────────────────────────────────────────────────┐
│  Logs                                        [Live ●] [Export] │
├────────────────────────────────────────────────────────────────┤
│ Filters:                                                        │
│ [Time: Last 1 hour ▼] [App: All ▼] [Severity: All ▼]           │
│ [🔍 Search logs...                                     ]       │
├────────────────────────────────────────────────────────────────┤
│ Results: 1,234 logs                                    [Prev][Next]│
│ ┌──────────────────────────────────────────────────────────┐   │
│ │ ▸ 10:23:45 ERROR WebAPI   Database connection failed     │   │
│ │ ▸ 10:23:44 INFO  WebAPI   Request completed: GET /users  │   │
│ │ ▾ 10:23:43 WARN  Worker   Queue length exceeds threshold │   │
│ │   ├─ Message: Queue length (150) exceeds threshold (100) │   │
│ │   ├─ Service: BackgroundWorker                           │   │
│ │   ├─ Attributes:                                         │   │
│ │   │    queue_name: "orders"                              │   │
│ │   │    current_length: 150                               │   │
│ │   │    threshold: 100                                    │   │
│ │   └─ [View Trace] [Copy JSON]                            │   │
│ │ ▸ 10:23:42 INFO  Frontend Page view: /dashboard          │   │
│ └──────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────┘
```

### 3. Metrics Page
Metrics visualization with charts.

**Features:**
- Metric name filter
- Time range selector
- Line/bar chart visualization
- Metric aggregation (sum, avg, min, max)
- Group by attributes
- Export data as CSV

**Layout:**
```
┌────────────────────────────────────────────────────────────────┐
│  Metrics                                             [Export]  │
├────────────────────────────────────────────────────────────────┤
│ [Time: Last 6 hours ▼] [App: WebAPI ▼] [Metric: Select... ▼]  │
│ [Aggregation: Average ▼] [Group by: None ▼]                    │
├────────────────────────────────────────────────────────────────┤
│ http_request_duration_ms                                       │
│ ┌────────────────────────────────────────────────────────┐    │
│ │                          ___                            │    │
│ │                    _____/   \__                         │    │
│ │              _____/            \____                    │    │
│ │         ____/                       \____               │    │
│ │    ____/                                  \___          │    │
│ │___/                                           \____     │    │
│ └────────────────────────────────────────────────────────┘    │
│   06:00   08:00   10:00   12:00   14:00   16:00              │
│                                                                │
│ Statistics: Min: 12ms | Avg: 45ms | Max: 234ms | P95: 89ms    │
└────────────────────────────────────────────────────────────────┘
```

### 4. Applications Page
Manage and view registered applications.

**Features:**
- List all applications
- View application details
- Generate/rotate API keys
- View application statistics
- Set application tags

### 5. Settings Page
User and system configuration.

**Features:**
- Timezone preferences
- Default time range
- Theme (light/dark)
- Data retention settings

---

## API Integration

### Endpoints Consumed

```typescript
// API Client
const api = {
  // Logs
  getLogs: (params: LogQueryParams) => Promise<PagedResult<LogEntry>>,
  getLogById: (id: string) => Promise<LogEntry>,

  // Metrics
  getMetrics: (params: MetricQueryParams) => Promise<MetricDataPoint[]>,
  getMetricNames: () => Promise<string[]>,

  // Applications
  getApplications: () => Promise<Application[]>,
  getApplication: (id: string) => Promise<Application>,
  createApplication: (data: CreateAppRequest) => Promise<Application>,
  regenerateApiKey: (id: string) => Promise<{ apiKey: string }>,

  // Health
  getHealth: () => Promise<HealthStatus>,
};

// Types
interface LogQueryParams {
  startTime: Date;
  endTime: Date;
  applicationIds?: string[];
  severities?: string[];
  search?: string;
  traceId?: string;
  pageSize?: number;
  continuationToken?: string;
}

interface LogEntry {
  id: string;
  timestamp: Date;
  severity: 'TRACE' | 'DEBUG' | 'INFO' | 'WARN' | 'ERROR' | 'FATAL';
  message: string;
  applicationId: string;
  applicationName: string;
  serviceName: string;
  traceId?: string;
  spanId?: string;
  attributes: Record<string, unknown>;
}

interface MetricQueryParams {
  startTime: Date;
  endTime: Date;
  metricName: string;
  applicationId?: string;
  aggregation?: 'sum' | 'avg' | 'min' | 'max' | 'count';
  groupBy?: string;
  interval?: string; // '1m', '5m', '1h', '1d'
}
```

---

## Component Library

### Core Components

1. **TimeRangeSelector**
   - Presets: Last 15min, 1h, 6h, 24h, 7d, 30d
   - Custom date/time picker
   - Relative to absolute conversion

2. **LogViewer**
   - Virtual scrolling for large datasets
   - Expandable log entries
   - Syntax highlighting for JSON
   - Severity badges with colors

3. **MetricChart**
   - Line and bar chart support
   - Zoom and pan
   - Tooltip with values
   - Legend with toggle

4. **FilterBar**
   - Multi-select dropdowns
   - Search input with debounce
   - Clear all button
   - Saved filters

5. **ApplicationCard**
   - Health indicator
   - Key metrics summary
   - Quick action buttons

---

## State Management

Using TanStack Query for server state:

```typescript
// hooks/useLogs.ts
export function useLogs(params: LogQueryParams) {
  return useInfiniteQuery({
    queryKey: ['logs', params],
    queryFn: ({ pageParam }) =>
      api.getLogs({ ...params, continuationToken: pageParam }),
    getNextPageParam: (lastPage) => lastPage.continuationToken,
    refetchInterval: params.isLive ? 5000 : false,
  });
}

// hooks/useMetrics.ts
export function useMetrics(params: MetricQueryParams) {
  return useQuery({
    queryKey: ['metrics', params],
    queryFn: () => api.getMetrics(params),
    enabled: !!params.metricName,
  });
}
```

---

## Responsive Design

| Breakpoint | Layout |
|------------|--------|
| Mobile (<640px) | Single column, stacked cards |
| Tablet (640-1024px) | 2-column grid |
| Desktop (>1024px) | Full layout with sidebar |

---

## Accessibility

- WCAG 2.1 AA compliance
- Keyboard navigation
- Screen reader support
- High contrast mode support
- Focus indicators
- ARIA labels

---

## Performance Requirements

| Metric | Target |
|--------|--------|
| Initial load (LCP) | <2.5s |
| Time to interactive | <3.5s |
| Bundle size (gzipped) | <200KB |
| Log list rendering (1000 items) | <100ms |
| Chart rendering | <200ms |

---

## Color Palette

### Severity Colors
| Severity | Light Mode | Dark Mode |
|----------|------------|-----------|
| TRACE | gray-400 | gray-500 |
| DEBUG | blue-400 | blue-500 |
| INFO | green-500 | green-400 |
| WARN | yellow-500 | yellow-400 |
| ERROR | red-500 | red-400 |
| FATAL | purple-600 | purple-400 |

### Status Colors
| Status | Color |
|--------|-------|
| Healthy | green-500 |
| Warning | yellow-500 |
| Error | red-500 |
| Unknown | gray-400 |
