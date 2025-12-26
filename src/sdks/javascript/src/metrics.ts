import type { ApmOptions, MetricAttributes, MetricRecord, KeyValue, AnyValue } from './types';

export class ApmMetrics {
  private options: Required<Pick<ApmOptions, 'endpoint' | 'applicationName'>> & ApmOptions;
  private queue: MetricRecord[] = [];
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

  counter(name: string, value: number, attributes?: MetricAttributes): void {
    this.record('counter', name, value, attributes);
  }

  gauge(name: string, value: number, attributes?: MetricAttributes): void {
    this.record('gauge', name, value, attributes);
  }

  histogram(name: string, value: number, attributes?: MetricAttributes): void {
    this.record('histogram', name, value, attributes);
  }

  private record(
    type: 'gauge' | 'counter' | 'histogram',
    name: string,
    value: number,
    attributes?: MetricAttributes
  ): void {
    const record: MetricRecord = {
      name,
      type,
      value,
      timestamp: Date.now() * 1_000_000,
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

    // Group by metric name
    const grouped = new Map<string, MetricRecord[]>();
    for (const record of records) {
      const existing = grouped.get(record.name) || [];
      existing.push(record);
      grouped.set(record.name, existing);
    }

    const metrics = Array.from(grouped.entries()).map(([name, recs]) => {
      const first = recs[0];
      const dataPoints = recs.map((r) => ({
        timeUnixNano: r.timestamp,
        asDouble: r.value,
        attributes: r.attributes,
      }));

      if (first.type === 'counter') {
        return {
          name,
          sum: {
            dataPoints,
            isMonotonic: true,
            aggregationTemporality: 2,
          },
        };
      }

      return {
        name,
        gauge: { dataPoints },
      };
    });

    const request = {
      resourceMetrics: [
        {
          resource: {
            attributes: [
              { key: 'service.name', value: { stringValue: this.options.applicationName } },
            ],
          },
          scopeMetrics: [
            {
              scope: { name: '@racelogic/apm-sdk' },
              metrics,
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

      const response = await fetch(`${this.options.endpoint}/v1/metrics`, {
        method: 'POST',
        headers,
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        this.queue.unshift(...records);
      }
    } catch {
      this.queue.unshift(...records);
    }
  }

  private convertAttributes(attributes?: MetricAttributes): KeyValue[] | undefined {
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
