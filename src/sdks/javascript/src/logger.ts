import type { ApmOptions, LogAttributes, LogRecord, KeyValue, AnyValue } from './types';

const SEVERITY_MAP: Record<string, { number: number; text: string }> = {
  trace: { number: 1, text: 'TRACE' },
  debug: { number: 5, text: 'DEBUG' },
  info: { number: 9, text: 'INFO' },
  warn: { number: 13, text: 'WARN' },
  error: { number: 17, text: 'ERROR' },
  fatal: { number: 21, text: 'FATAL' },
};

export class ApmLogger {
  private options: Required<Pick<ApmOptions, 'endpoint' | 'applicationName'>> & ApmOptions;
  private queue: LogRecord[] = [];
  private flushTimer: ReturnType<typeof setInterval> | null = null;

  constructor(options: ApmOptions) {
    this.options = {
      batchSize: 100,
      flushIntervalMs: 5000,
      maxRetries: 3,
      environment: 'development',
      ...options,
    };

    this.startFlushTimer();
  }

  trace(message: string, attributes?: LogAttributes): void {
    this.log('trace', message, undefined, attributes);
  }

  debug(message: string, attributes?: LogAttributes): void {
    this.log('debug', message, undefined, attributes);
  }

  info(message: string, attributes?: LogAttributes): void {
    this.log('info', message, undefined, attributes);
  }

  warn(message: string, attributes?: LogAttributes): void {
    this.log('warn', message, undefined, attributes);
  }

  error(message: string, error?: Error, attributes?: LogAttributes): void {
    this.log('error', message, error, attributes);
  }

  fatal(message: string, error?: Error, attributes?: LogAttributes): void {
    this.log('fatal', message, error, attributes);
  }

  private log(
    level: keyof typeof SEVERITY_MAP,
    message: string,
    error?: Error,
    attributes?: LogAttributes
  ): void {
    const severity = SEVERITY_MAP[level];
    const body = error ? `${message}\n${error.stack || error.message}` : message;

    const record: LogRecord = {
      timeUnixNano: Date.now() * 1_000_000,
      severityNumber: severity.number,
      severityText: severity.text,
      body,
      attributes: this.convertAttributes(attributes),
    };

    this.queue.push(record);

    if (this.queue.length >= (this.options.batchSize || 100)) {
      this.flush();
    }
  }

  async flush(): Promise<void> {
    if (this.queue.length === 0) return;

    const records = this.queue.splice(0, this.options.batchSize || 100);

    const request = {
      resourceLogs: [
        {
          resource: {
            attributes: [
              { key: 'service.name', value: { stringValue: this.options.applicationName } },
              { key: 'service.version', value: { stringValue: this.options.serviceVersion || '1.0.0' } },
              { key: 'deployment.environment', value: { stringValue: this.options.environment } },
            ],
          },
          scopeLogs: [
            {
              scope: { name: '@racelogic/apm-sdk' },
              logRecords: records,
            },
          ],
        },
      ],
    };

    try {
      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
      };

      if (this.options.apiKey) {
        headers['X-API-Key'] = this.options.apiKey;
      }

      if (this.options.applicationId) {
        headers['X-Application-Id'] = this.options.applicationId;
      }

      const response = await fetch(`${this.options.endpoint}/v1/logs`, {
        method: 'POST',
        headers,
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        // Re-queue on failure
        this.queue.unshift(...records);
      }
    } catch {
      // Re-queue on failure
      this.queue.unshift(...records);
    }
  }

  private convertAttributes(attributes?: LogAttributes): KeyValue[] | undefined {
    if (!attributes) return undefined;

    return Object.entries(attributes)
      .filter(([, v]) => v !== undefined)
      .map(([key, value]) => ({
        key,
        value: this.toAnyValue(value),
      }));
  }

  private toAnyValue(value: string | number | boolean | undefined): AnyValue {
    if (typeof value === 'string') return { stringValue: value };
    if (typeof value === 'number') {
      return Number.isInteger(value) ? { intValue: value } : { doubleValue: value };
    }
    if (typeof value === 'boolean') return { boolValue: value };
    return { stringValue: String(value) };
  }

  private startFlushTimer(): void {
    this.flushTimer = setInterval(() => {
      this.flush();
    }, this.options.flushIntervalMs || 5000);
  }

  async shutdown(): Promise<void> {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
      this.flushTimer = null;
    }
    await this.flush();
  }
}
