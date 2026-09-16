$ErrorActionPreference = "Stop"

Write-Host "Construindo e iniciando API e Mailpit..." -ForegroundColor Cyan
docker compose up --build --detach

Write-Host "Swagger: http://localhost:5080/swagger" -ForegroundColor Green
Write-Host "Mailpit: http://localhost:8025" -ForegroundColor Green
Write-Host "Use '.\scripts\windows\logs-docker.ps1' para acompanhar os logs."
