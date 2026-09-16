param(
    [string]$BaseUrl = "http://localhost:5080"
)

$ErrorActionPreference = "Stop"

Write-Host "Testando API em $BaseUrl..." -ForegroundColor Cyan
Invoke-RestMethod "$BaseUrl/health"
Invoke-RestMethod "$BaseUrl/health/database"
