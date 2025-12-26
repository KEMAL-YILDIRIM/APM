# Sample Express.js App

This is a sample Express.js application demonstrating APM SDK integration.

## Quick Start

1. Make sure the APM Collector is running at `http://localhost:5000`

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the application:
   ```bash
   npm start
   ```

4. The app will run at `http://localhost:3001`

## Available Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | Welcome message |
| `/users` | GET | List all users |
| `/users/:id` | GET | Get user by ID |
| `/users` | POST | Create new user |
| `/error` | GET | Trigger an error (for testing) |
| `/slow` | GET | Slow operation (500-2000ms) |
| `/health` | GET | Health check |

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `APM_ENDPOINT` | APM Collector URL | `http://localhost:5000` |
| `APM_API_KEY` | API key for authentication | - |
| `PORT` | Server port | `3001` |
| `NODE_ENV` | Environment name | `development` |

## Testing Telemetry

```bash
# Generate some traffic
curl http://localhost:3001/
curl http://localhost:3001/users
curl http://localhost:3001/users/1
curl -X POST http://localhost:3001/users -H "Content-Type: application/json" -d '{"name":"Test","email":"test@example.com"}'
curl http://localhost:3001/error
curl http://localhost:3001/slow
```

Then check the APM Dashboard at `http://localhost:3000` to see the logs and metrics.
