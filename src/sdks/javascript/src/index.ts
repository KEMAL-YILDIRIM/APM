import { ApmLogger } from './logger';
import { ApmMetrics } from './metrics';
import type { ApmOptions } from './types';

export { ApmLogger } from './logger';
export { ApmMetrics } from './metrics';
export type { ApmOptions, LogAttributes, MetricAttributes, LogLevel } from './types';

/**
 * APM Client for JavaScript/TypeScript applications.
 */
export class ApmClient {
  private static instance: ApmClient | null = null;

  public readonly logger: ApmLogger;
  public readonly metrics: ApmMetrics;
  private readonly options: ApmOptions;

  private constructor(options: ApmOptions) {
    this.options = options;
    this.logger = new ApmLogger(options);
    this.metrics = new ApmMetrics(options);
  }

  /**
   * Initialize the APM client with the given options.
   * Can only be called once - subsequent calls return the existing instance.
   */
  static init(options: ApmOptions): ApmClient {
    if (!ApmClient.instance) {
      ApmClient.instance = new ApmClient(options);
    }
    return ApmClient.instance;
  }

  /**
   * Get the current APM client instance.
   * Throws if init() hasn't been called.
   */
  static getInstance(): ApmClient {
    if (!ApmClient.instance) {
      throw new Error('ApmClient not initialized. Call ApmClient.init() first.');
    }
    return ApmClient.instance;
  }

  /**
   * Get the logger instance.
   */
  getLogger(): ApmLogger {
    return this.logger;
  }

  /**
   * Get the metrics instance.
   */
  getMetrics(): ApmMetrics {
    return this.metrics;
  }

  /**
   * Flush all pending telemetry.
   */
  async flush(): Promise<void> {
    await Promise.all([this.logger.flush(), this.metrics.flush()]);
  }

  /**
   * Shutdown the APM client and flush all pending telemetry.
   */
  async shutdown(): Promise<void> {
    await Promise.all([this.logger.shutdown(), this.metrics.shutdown()]);
    ApmClient.instance = null;
  }

  /**
   * Express middleware for automatic request tracing.
   */
  expressMiddleware() {
    return (
      req: { method: string; path: string; url: string },
      res: { statusCode: number; on: (event: string, cb: () => void) => void },
      next: () => void
    ) => {
      const start = Date.now();

      res.on('finish', () => {
        const duration = Date.now() - start;
        this.metrics.histogram('http_request_duration_ms', duration, {
          method: req.method,
          path: req.path || req.url,
          status: res.statusCode,
        });

        this.logger.info(`${req.method} ${req.path || req.url}`, {
          method: req.method,
          path: req.path || req.url,
          statusCode: res.statusCode,
          durationMs: duration,
        });
      });

      next();
    };
  }
}

export default ApmClient;
