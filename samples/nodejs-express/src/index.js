import express from 'express';

// Import the APM SDK (in production, use: import { ApmClient } from '@racelogic/apm-sdk')
// For local development, we'll inline a simplified version
const ApmClient = createApmClient();

// Initialize APM
const apm = ApmClient.init({
  endpoint: process.env.APM_ENDPOINT || 'http://localhost:5000',
  apiKey: process.env.APM_API_KEY,
  applicationId: 'sample-express-app',
  applicationName: 'Sample Express App',
  environment: process.env.NODE_ENV || 'development',
  serviceVersion: '1.0.0',
});

const app = express();
app.use(express.json());

// APM middleware for automatic request logging
app.use((req, res, next) => {
  const start = Date.now();

  res.on('finish', () => {
    const duration = Date.now() - start;
    apm.metrics.histogram('http_request_duration_ms', duration, {
      method: req.method,
      path: req.path,
      status: res.statusCode,
    });

    const logLevel = res.statusCode >= 500 ? 'error' : res.statusCode >= 400 ? 'warn' : 'info';
    apm.logger[logLevel](`${req.method} ${req.path}`, {
      method: req.method,
      path: req.path,
      statusCode: res.statusCode,
      durationMs: duration,
    });
  });

  next();
});

// Routes
app.get('/', (req, res) => {
  apm.logger.info('Home page accessed');
  res.json({ message: 'Welcome to Sample Express App', timestamp: new Date().toISOString() });
});

app.get('/users', (req, res) => {
  const start = Date.now();

  apm.logger.info('Fetching users list');

  // Simulate some work
  const users = Array.from({ length: 10 }, (_, i) => ({
    id: i + 1,
    name: `User ${i + 1}`,
    email: `user${i + 1}@example.com`,
  }));

  const duration = Date.now() - start;
  apm.metrics.histogram('get_users_duration_ms', duration);
  apm.metrics.gauge('users_count', users.length);

  apm.logger.debug('Returned users', { count: users.length });

  res.json(users);
});

app.get('/users/:id', (req, res) => {
  const id = parseInt(req.params.id, 10);

  apm.logger.info('Fetching user', { userId: id });
  apm.metrics.counter('user_fetch_count', 1, { user_id: id });

  if (id <= 0 || id > 100) {
    apm.logger.warn('User not found', { userId: id });
    return res.status(404).json({ error: 'User not found' });
  }

  res.json({
    id,
    name: `User ${id}`,
    email: `user${id}@example.com`,
    createdAt: new Date(Date.now() - id * 24 * 60 * 60 * 1000).toISOString(),
  });
});

app.post('/users', (req, res) => {
  const { name, email } = req.body;

  apm.logger.info('Creating new user', { name, email });
  apm.metrics.counter('user_created_count', 1);

  const newUser = {
    id: Math.floor(Math.random() * 900) + 100,
    name,
    email,
    createdAt: new Date().toISOString(),
  };

  apm.logger.info('User created successfully', { userId: newUser.id });

  res.status(201).json(newUser);
});

app.get('/error', (req, res) => {
  const error = new Error('This is a test error');
  apm.logger.error('Intentional error for testing', error);
  res.status(500).json({ error: 'Something went wrong!' });
});

app.get('/slow', async (req, res) => {
  const start = Date.now();

  apm.logger.info('Starting slow operation');

  // Simulate slow operation
  await new Promise((resolve) => setTimeout(resolve, Math.random() * 1500 + 500));

  const duration = Date.now() - start;
  apm.metrics.histogram('slow_operation_duration_ms', duration);

  apm.logger.info('Slow operation completed', { durationMs: duration });

  res.json({ message: 'Slow operation completed', durationMs: duration });
});

app.get('/health', (req, res) => {
  res.json({ status: 'healthy', timestamp: new Date().toISOString() });
});

// Graceful shutdown
process.on('SIGTERM', async () => {
  console.log('Shutting down...');
  await apm.flush();
  await apm.shutdown();
  process.exit(0);
});

const PORT = process.env.PORT || 3001;
app.listen(PORT, () => {
  console.log(`Sample Express App running at http://localhost:${PORT}`);
  apm.logger.info('Application started', { port: PORT });
});


// Simplified inline APM client for demo purposes
// In production, import from '@racelogic/apm-sdk'
function createApmClient() {
  class SimpleLogger {
    constructor(options) {
      this.options = options;
      this.queue = [];
      this.flushInterval = setInterval(() => this.flush(), options.flushIntervalMs || 5000);
    }

    log(level, severityNumber, message, attributes) {
      const record = {
        timeUnixNano: Date.now() * 1_000_000,
        severityNumber,
        severityText: level.toUpperCase(),
        body: message instanceof Error ? `${message.message}\n${message.stack}` : message,
        attributes: attributes ? Object.entries(attributes).map(([k, v]) => ({
          key: k,
          value: { stringValue: String(v) }
        })) : undefined,
      };
      this.queue.push(record);
      if (this.queue.length >= (this.options.batchSize || 100)) this.flush();
    }

    trace(msg, attrs) { this.log('trace', 1, msg, attrs); }
    debug(msg, attrs) { this.log('debug', 5, msg, attrs); }
    info(msg, attrs) { this.log('info', 9, msg, attrs); }
    warn(msg, attrs) { this.log('warn', 13, msg, attrs); }
    error(msg, err, attrs) {
      const finalAttrs = err instanceof Error ? { ...attrs, error: err.message } : attrs;
      this.log('error', 17, err instanceof Error ? `${msg}\n${err.stack}` : msg, finalAttrs);
    }
    fatal(msg, err, attrs) { this.log('fatal', 21, msg, attrs); }

    async flush() {
      if (this.queue.length === 0) return;
      const records = this.queue.splice(0, 100);
      const request = {
        resourceLogs: [{
          resource: {
            attributes: [
              { key: 'service.name', value: { stringValue: this.options.applicationName } },
              { key: 'deployment.environment', value: { stringValue: this.options.environment || 'development' } },
            ]
          },
          scopeLogs: [{ scope: { name: '@racelogic/apm-sdk' }, logRecords: records }]
        }]
      };
      try {
        const headers = { 'Content-Type': 'application/json' };
        if (this.options.apiKey) headers['X-API-Key'] = this.options.apiKey;
        if (this.options.applicationId) headers['X-Application-Id'] = this.options.applicationId;
        await fetch(`${this.options.endpoint}/v1/logs`, {
          method: 'POST', headers, body: JSON.stringify(request)
        });
      } catch (e) { records.forEach(r => this.queue.push(r)); }
    }

    async shutdown() { clearInterval(this.flushInterval); await this.flush(); }
  }

  class SimpleMetrics {
    constructor(options) {
      this.options = options;
      this.queue = [];
      this.flushInterval = setInterval(() => this.flush(), options.flushIntervalMs || 5000);
    }

    record(type, name, value, attrs) {
      this.queue.push({ type, name, value, timestamp: Date.now() * 1_000_000, attributes: attrs });
      if (this.queue.length >= (this.options.batchSize || 100)) this.flush();
    }

    counter(name, value, attrs) { this.record('counter', name, value, attrs); }
    gauge(name, value, attrs) { this.record('gauge', name, value, attrs); }
    histogram(name, value, attrs) { this.record('histogram', name, value, attrs); }

    async flush() {
      if (this.queue.length === 0) return;
      const records = this.queue.splice(0, 100);
      const grouped = {};
      records.forEach(r => { (grouped[r.name] = grouped[r.name] || []).push(r); });

      const metrics = Object.entries(grouped).map(([name, recs]) => ({
        name,
        gauge: {
          dataPoints: recs.map(r => ({
            timeUnixNano: r.timestamp,
            asDouble: r.value,
            attributes: r.attributes ? Object.entries(r.attributes).map(([k, v]) => ({
              key: k, value: { stringValue: String(v) }
            })) : undefined
          }))
        }
      }));

      const request = {
        resourceMetrics: [{
          resource: {
            attributes: [{ key: 'service.name', value: { stringValue: this.options.applicationName } }]
          },
          scopeMetrics: [{ scope: { name: '@racelogic/apm-sdk' }, metrics }]
        }]
      };

      try {
        const headers = { 'Content-Type': 'application/json' };
        if (this.options.apiKey) headers['X-API-Key'] = this.options.apiKey;
        if (this.options.applicationId) headers['X-Application-Id'] = this.options.applicationId;
        await fetch(`${this.options.endpoint}/v1/metrics`, {
          method: 'POST', headers, body: JSON.stringify(request)
        });
      } catch (e) { records.forEach(r => this.queue.push(r)); }
    }

    async shutdown() { clearInterval(this.flushInterval); await this.flush(); }
  }

  return {
    init(options) {
      const logger = new SimpleLogger(options);
      const metrics = new SimpleMetrics(options);
      return {
        logger,
        metrics,
        async flush() { await logger.flush(); await metrics.flush(); },
        async shutdown() { await logger.shutdown(); await metrics.shutdown(); }
      };
    }
  };
}
