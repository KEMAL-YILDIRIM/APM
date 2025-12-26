# APM Sample Applications

This directory contains sample applications demonstrating how to integrate the APM SDK in different languages and frameworks.

## Available Samples

| Sample | Language | Framework | Port |
|--------|----------|-----------|------|
| [dotnet-webapi](./dotnet-webapi) | C# | ASP.NET Core 8 | 5001 |
| [nodejs-express](./nodejs-express) | JavaScript | Express.js | 3001 |
| [python-flask](./python-flask) | Python | Flask | 3002 |

## Prerequisites

1. **APM Collector** running at `http://localhost:5000`
   ```bash
   cd src/backend/APM.Collector
   dotnet run
   ```

2. **APM Dashboard** (optional, for viewing telemetry)
   ```bash
   cd src/frontend
   npm install && npm run dev
   ```

## Quick Start

### .NET Web API

```bash
cd samples/dotnet-webapi
dotnet run
```

### Node.js Express

```bash
cd samples/nodejs-express
npm install
npm start
```

### Python Flask

```bash
cd samples/python-flask
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate
pip install -r requirements.txt
python app.py
```

## Generate Test Traffic

Run the load test script to generate sample telemetry data:

```bash
# Using the included script
./generate-traffic.sh

# Or manually
curl http://localhost:5001/users
curl http://localhost:3001/users
curl http://localhost:3002/users
```

## Common Endpoints

All sample apps implement the same endpoints:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | Welcome message |
| `/users` | GET | List users (logs + metrics) |
| `/users/{id}` | GET | Get user by ID |
| `/users` | POST | Create user |
| `/error` | GET | Trigger test error |
| `/slow` | GET | Slow operation (metrics) |
| `/health` | GET | Health check |

## Telemetry Generated

Each request generates:
- **Logs**: Request info, errors, debug messages
- **Metrics**: Request duration, counters, gauges

View the telemetry in the APM Dashboard at `http://localhost:3000`.
