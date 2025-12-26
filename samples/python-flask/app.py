"""
Sample Flask application demonstrating APM SDK integration.
"""

import os
import sys
import time
import random
import atexit
from datetime import datetime, timedelta
from flask import Flask, jsonify, request

# Add the SDK to path for local development
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..', 'src', 'sdks', 'python', 'src'))

from racelogic_apm import ApmClient

# Initialize APM
apm = ApmClient(
    endpoint=os.environ.get('APM_ENDPOINT', 'http://localhost:5000'),
    api_key=os.environ.get('APM_API_KEY'),
    application_id='sample-flask-app',
    application_name='Sample Flask App',
    environment=os.environ.get('FLASK_ENV', 'development'),
    service_version='1.0.0',
)

app = Flask(__name__)


# Middleware for automatic request logging
@app.before_request
def before_request():
    request.start_time = time.time()


@app.after_request
def after_request(response):
    duration_ms = (time.time() - request.start_time) * 1000

    apm.metrics.histogram(
        'http_request_duration_ms',
        duration_ms,
        method=request.method,
        path=request.path,
        status=response.status_code,
    )

    log_level = 'error' if response.status_code >= 500 else 'warn' if response.status_code >= 400 else 'info'
    getattr(apm.logger, log_level)(
        f'{request.method} {request.path}',
        method=request.method,
        path=request.path,
        status_code=response.status_code,
        duration_ms=round(duration_ms, 2),
    )

    return response


@app.route('/')
def home():
    apm.logger.info('Home page accessed')
    return jsonify({
        'message': 'Welcome to Sample Flask App',
        'timestamp': datetime.utcnow().isoformat()
    })


@app.route('/users')
def get_users():
    start = time.time()

    apm.logger.info('Fetching users list')

    # Simulate some work
    users = [
        {'id': i, 'name': f'User {i}', 'email': f'user{i}@example.com'}
        for i in range(1, 11)
    ]

    duration = (time.time() - start) * 1000
    apm.metrics.histogram('get_users_duration_ms', duration)
    apm.metrics.gauge('users_count', len(users))

    apm.logger.debug('Returned users', count=len(users))

    return jsonify(users)


@app.route('/users/<int:user_id>')
def get_user(user_id):
    apm.logger.info('Fetching user', user_id=user_id)
    apm.metrics.counter('user_fetch_count', 1, user_id=user_id)

    if user_id <= 0 or user_id > 100:
        apm.logger.warn('User not found', user_id=user_id)
        return jsonify({'error': 'User not found'}), 404

    return jsonify({
        'id': user_id,
        'name': f'User {user_id}',
        'email': f'user{user_id}@example.com',
        'createdAt': (datetime.utcnow() - timedelta(days=user_id)).isoformat()
    })


@app.route('/users', methods=['POST'])
def create_user():
    data = request.get_json() or {}
    name = data.get('name', 'Unknown')
    email = data.get('email', 'unknown@example.com')

    apm.logger.info('Creating new user', name=name, email=email)
    apm.metrics.counter('user_created_count', 1)

    new_user = {
        'id': random.randint(100, 999),
        'name': name,
        'email': email,
        'createdAt': datetime.utcnow().isoformat()
    }

    apm.logger.info('User created successfully', user_id=new_user['id'])

    return jsonify(new_user), 201


@app.route('/error')
def trigger_error():
    try:
        raise ValueError('This is a test error')
    except Exception as e:
        apm.logger.error('Intentional error for testing', exception=e)
        return jsonify({'error': 'Something went wrong!'}), 500


@app.route('/slow')
def slow_operation():
    start = time.time()

    apm.logger.info('Starting slow operation')

    # Simulate slow operation
    time.sleep(random.uniform(0.5, 2.0))

    duration_ms = (time.time() - start) * 1000
    apm.metrics.histogram('slow_operation_duration_ms', duration_ms)

    apm.logger.info('Slow operation completed', duration_ms=round(duration_ms, 2))

    return jsonify({
        'message': 'Slow operation completed',
        'durationMs': round(duration_ms, 2)
    })


@app.route('/health')
def health():
    return jsonify({
        'status': 'healthy',
        'timestamp': datetime.utcnow().isoformat()
    })


# Decorated function example
@apm.trace('calculate_something')
def calculate_something(n):
    """Example of using the @apm.trace decorator."""
    time.sleep(0.1)  # Simulate work
    return n * 2


@app.route('/calculate/<int:n>')
def calculate(n):
    result = calculate_something(n)
    return jsonify({'input': n, 'result': result})


if __name__ == '__main__':
    port = int(os.environ.get('PORT', 3002))
    print(f'Sample Flask App running at http://localhost:{port}')
    apm.logger.info('Application started', port=port)
    app.run(host='0.0.0.0', port=port, debug=True)
