# PowerShell script to generate test traffic for APM sample apps

param(
    [int]$Iterations = 10,
    [int]$DelayMs = 500
)

$endpoints = @(
    # .NET Web API (port 5001)
    @{ Name = ".NET"; BaseUrl = "http://localhost:5001" },
    # Node.js Express (port 3001)
    @{ Name = "Node.js"; BaseUrl = "http://localhost:3001" },
    # Python Flask (port 3002)
    @{ Name = "Python"; BaseUrl = "http://localhost:3002" }
)

$paths = @(
    @{ Method = "GET"; Path = "/" },
    @{ Method = "GET"; Path = "/users" },
    @{ Method = "GET"; Path = "/users/1" },
    @{ Method = "GET"; Path = "/users/5" },
    @{ Method = "GET"; Path = "/users/999" },  # Not found
    @{ Method = "POST"; Path = "/users"; Body = '{"name":"Test User","email":"test@example.com"}' },
    @{ Method = "GET"; Path = "/slow" },
    @{ Method = "GET"; Path = "/error" },
    @{ Method = "GET"; Path = "/health" }
)

Write-Host "APM Traffic Generator" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host "Iterations: $Iterations"
Write-Host "Delay: $DelayMs ms"
Write-Host ""

for ($i = 1; $i -le $Iterations; $i++) {
    Write-Host "Iteration $i of $Iterations" -ForegroundColor Yellow

    foreach ($endpoint in $endpoints) {
        $baseUrl = $endpoint.BaseUrl
        $name = $endpoint.Name

        # Check if server is running
        try {
            $null = Invoke-WebRequest -Uri "$baseUrl/health" -Method GET -TimeoutSec 2 -ErrorAction Stop
        }
        catch {
            Write-Host "  [$name] Server not running at $baseUrl" -ForegroundColor Red
            continue
        }

        foreach ($path in $paths) {
            $url = "$baseUrl$($path.Path)"
            $method = $path.Method

            try {
                $params = @{
                    Uri = $url
                    Method = $method
                    TimeoutSec = 10
                    ErrorAction = "Stop"
                }

                if ($path.Body) {
                    $params.Body = $path.Body
                    $params.ContentType = "application/json"
                }

                $response = Invoke-WebRequest @params
                $status = $response.StatusCode
                Write-Host "  [$name] $method $($path.Path) -> $status" -ForegroundColor Green
            }
            catch {
                $status = $_.Exception.Response.StatusCode.value__
                if ($status) {
                    Write-Host "  [$name] $method $($path.Path) -> $status" -ForegroundColor Yellow
                }
                else {
                    Write-Host "  [$name] $method $($path.Path) -> ERROR" -ForegroundColor Red
                }
            }

            Start-Sleep -Milliseconds ($DelayMs / 3)
        }
    }

    if ($i -lt $Iterations) {
        Start-Sleep -Milliseconds $DelayMs
    }
}

Write-Host ""
Write-Host "Traffic generation complete!" -ForegroundColor Cyan
Write-Host "Check the APM Dashboard at http://localhost:3000" -ForegroundColor Cyan
