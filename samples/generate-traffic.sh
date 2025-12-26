#!/bin/bash

# Bash script to generate test traffic for APM sample apps

ITERATIONS=${1:-10}
DELAY_MS=${2:-500}

echo "APM Traffic Generator"
echo "====================="
echo "Iterations: $ITERATIONS"
echo "Delay: ${DELAY_MS}ms"
echo ""

# Define endpoints
declare -a ENDPOINTS=(
    "http://localhost:5001:.NET"
    "http://localhost:3001:Node.js"
    "http://localhost:3002:Python"
)

# Define paths
declare -a PATHS=(
    "GET:/"
    "GET:/users"
    "GET:/users/1"
    "GET:/users/5"
    "GET:/users/999"
    "POST:/users"
    "GET:/slow"
    "GET:/error"
    "GET:/health"
)

make_request() {
    local base_url=$1
    local method=$2
    local path=$3
    local name=$4

    local url="${base_url}${path}"

    if [ "$method" == "POST" ] && [ "$path" == "/users" ]; then
        response=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$url" \
            -H "Content-Type: application/json" \
            -d '{"name":"Test User","email":"test@example.com"}' \
            --connect-timeout 2 --max-time 10 2>/dev/null)
    else
        response=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" "$url" \
            --connect-timeout 2 --max-time 10 2>/dev/null)
    fi

    if [ "$response" -ge 200 ] && [ "$response" -lt 400 ]; then
        echo -e "  [\033[0;32m$name\033[0m] $method $path -> $response"
    elif [ "$response" -ge 400 ] && [ "$response" -lt 500 ]; then
        echo -e "  [\033[0;33m$name\033[0m] $method $path -> $response"
    else
        echo -e "  [\033[0;31m$name\033[0m] $method $path -> $response"
    fi
}

check_server() {
    local base_url=$1
    curl -s -o /dev/null -w "%{http_code}" "${base_url}/health" --connect-timeout 2 2>/dev/null
}

for ((i=1; i<=ITERATIONS; i++)); do
    echo -e "\033[0;33mIteration $i of $ITERATIONS\033[0m"

    for endpoint in "${ENDPOINTS[@]}"; do
        IFS=':' read -r base_url name <<< "$endpoint"
        base_url="${base_url}:${name%%:*}"
        name="${endpoint##*:}"

        # Check if server is running
        status=$(check_server "$base_url")
        if [ "$status" != "200" ]; then
            echo -e "  [\033[0;31m$name\033[0m] Server not running at $base_url"
            continue
        fi

        for path_def in "${PATHS[@]}"; do
            IFS=':' read -r method path <<< "$path_def"
            make_request "$base_url" "$method" "$path" "$name"
            sleep $(echo "scale=3; $DELAY_MS / 3000" | bc)
        done
    done

    if [ $i -lt $ITERATIONS ]; then
        sleep $(echo "scale=3; $DELAY_MS / 1000" | bc)
    fi
done

echo ""
echo -e "\033[0;36mTraffic generation complete!\033[0m"
echo -e "\033[0;36mCheck the APM Dashboard at http://localhost:3000\033[0m"
