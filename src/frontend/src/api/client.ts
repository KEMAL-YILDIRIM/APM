import axios from 'axios';

const API_BASE = import.meta.env.VITE_API_URL || '';

export const api = axios.create({
  baseURL: API_BASE,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Types
export interface LogEntry {
  id: string;
  timestamp: string;
  severity: string;
  severityNumber: number;
  message: string;
  applicationId: string;
  applicationName: string;
  serviceName?: string;
  traceId?: string;
  spanId?: string;
  attributes?: Record<string, unknown>;
  resourceAttributes?: Record<string, unknown>;
}

export interface MetricEntry {
  id: string;
  timestamp: string;
  name: string;
  description?: string;
  unit?: string;
  type: string;
  value: number;
  min?: number;
  max?: number;
  sum?: number;
  count?: number;
  applicationId: string;
  applicationName: string;
  attributes?: Record<string, unknown>;
}

export interface Application {
  id: string;
  name: string;
  environment?: string;
  createdAt: string;
  lastSeenAt: string;
  isActive: boolean;
  tags?: Record<string, unknown>;
}

export interface LogQueryParams {
  applicationId?: string;
  startTime?: string;
  endTime?: string;
  severity?: string;
  search?: string;
  limit?: number;
}

export interface MetricQueryParams {
  applicationId?: string;
  name?: string;
  startTime?: string;
  endTime?: string;
  aggregation?: string;
  limit?: number;
}

// API functions
export async function getLogs(params: LogQueryParams = {}) {
  const response = await api.get<{ data: LogEntry[]; count: number }>('/api/logs', { params });
  return response.data;
}

export async function getMetrics(params: MetricQueryParams = {}) {
  const response = await api.get<{ data: MetricEntry[]; count: number }>('/api/metrics', { params });
  return response.data;
}

export async function getMetricNames(applicationId?: string) {
  const response = await api.get<{ names: string[] }>('/api/metrics/names', {
    params: { applicationId },
  });
  return response.data.names;
}

export async function getApplications() {
  const response = await api.get<{ data: Application[] }>('/api/applications');
  return response.data.data;
}

export async function getApplication(id: string) {
  const response = await api.get<Application>(`/api/applications/${id}`);
  return response.data;
}

export async function createApplication(data: { name: string; environment?: string }) {
  const response = await api.post<Application & { apiKey: string }>('/api/applications', data);
  return response.data;
}

export async function regenerateApiKey(id: string) {
  const response = await api.post<{ apiKey: string }>(`/api/applications/${id}/regenerate-key`);
  return response.data;
}

export async function getHealth() {
  const response = await api.get<{ status: string; timestamp: string }>('/health');
  return response.data;
}
