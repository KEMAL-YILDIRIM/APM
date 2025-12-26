# Sample Flask App

This is a sample Flask application demonstrating APM SDK integration.

## Quick Start

1. Make sure the APM Collector is running at `http://localhost:5000`

2. Create a virtual environment and install dependencies:
   ```bash
   python -m venv venv
   source venv/bin/activate  # On Windows: venv\Scripts\activate
   pip install -r requirements.txt
   ```

3. Start the application:
   ```bash
   python app.py
   ```

4. The app will run at `http://localhost:3002`

## Available Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | Welcome message |
| `/users` | GET | List all users |
| `/users/<id>` | GET | Get user by ID |
| `/users` | POST | Create new user |
| `/error` | GET | Trigger an error (for testing) |
| `/slow` | GET | Slow operation (500-2000ms) |
| `/calculate/<n>` | GET | Example with @apm.trace decorator |
| `/health` | GET | Health check |

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `APM_ENDPOINT` | APM Collector URL | `http://localhost:5000` |
| `APM_API_KEY` | API key for authentication | - |
| `PORT` | Server port | `3002` |
| `FLASK_ENV` | Environment name | `development` |

## Testing Telemetry

```bash
# Generate some traffic
curl http://localhost:3002/
curl http://localhost:3002/users
curl http://localhost:3002/users/1
curl -X POST http://localhost:3002/users -H "Content-Type: application/json" -d '{"name":"Test","email":"test@example.com"}'
curl http://localhost:3002/error
curl http://localhost:3002/slow
curl http://localhost:3002/calculate/42
```

Then check the APM Dashboard at `http://localhost:3000` to see the logs and metrics.

## Using the SDK Decorator

The `@apm.trace` decorator can be used to automatically trace function execution:

```python
@apm.trace('my_function')
def my_function():
    # Your code here
    pass
```

This will automatically:
- Record the function duration as a histogram metric
- Log any exceptions that occur
