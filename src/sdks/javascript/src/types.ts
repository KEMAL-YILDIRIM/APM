export interface ApmOptions {
  /** The APM Collector endpoint URL */
  endpoint: string;
  /** The API key for authentication */
  apiKey?: string;
  /** The unique identifier for this application */
  applicationId?: string;
  /** The human-readable name for this application */
  applicationName: string;
  /** The environment (e.g., development, staging, production) */
  environment?: string;
  /** The service version */
  serviceVersion?: string;
  /** Number of records to batch before sending */
  batchSize?: number;
  /** Interval in milliseconds to flush the buffer */
  flushIntervalMs?: number;
  /** Maximum number of retry attempts for failed requests */
  maxRetries?: number;
}

export interface LogAttributes {
  [key: string]: string | number | boolean | undefined;
}

export interface MetricAttributes {
  [key: string]: string | number | boolean | undefined;
}

export type LogLevel = 'trace' | 'debug' | 'info' | 'warn' | 'error' | 'fatal';

export interface LogRecord {
  timeUnixNano: number;
  severityNumber: number;
  severityText: string;
  body: string;
  attributes?: KeyValue[];
  traceId?: string;
  spanId?: string;
}

export interface MetricRecord {
  name: string;
  type: 'gauge' | 'counter' | 'histogram';
  value: number;
  timestamp: number;
  attributes?: KeyValue[];
}

export interface KeyValue {
  key: string;
  value: AnyValue;
}

export interface AnyValue {
  stringValue?: string;
  intValue?: number;
  doubleValue?: number;
  boolValue?: boolean;
}
