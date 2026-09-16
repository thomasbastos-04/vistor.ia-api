$ErrorActionPreference = "Stop"

Write-Host "Restaurando e compilando a Vistor.ia API..." -ForegroundColor Cyan
dotnet restore
dotnet build --no-restore

Write-Host "API local: http://localhost:5082/swagger" -ForegroundColor Green
dotnet run --no-build --project .\src\VistoriaApi.Api\VistoriaApi.Api.csproj
