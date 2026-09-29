# Development guide

This guide explains how to set up the project for local development.

## Prerequisites

- .NET 8 SDK
- PostgreSQL
- (optional) Docker and Docker Compose
- (optional) Mailpit or similar local SMTP testing tool

## Environment

Copy example env (if present) and update values:

```powershell
Copy-Item .env.example .env
```

Important configuration keys:

- ConnectionStrings:Default — `Host=localhost;Database=vistoria;Username=...;Password=...`
- Jwt:Key — minimum 32 characters
- Smtp:Host, Smtp:Port, Smtp:From, (Smtp:Username, Smtp:Password)

## Run locally

1. Restore and build

   dotnet restore
   dotnet build

2. Run the API

   dotnet run --project src/VistoriaApi.Api/VistoriaApi.Api.csproj

3. Open Swagger: `http://localhost:5080/swagger` (port may vary by profile)

## Database migrations

If you use EF Core migrations:

  dotnet tool install --global dotnet-ef
  dotnet ef migrations add Initial -p src/VistoriaApi.Infrastructure -s src/VistoriaApi.Api
  dotnet ef database update -p src/VistoriaApi.Infrastructure -s src/VistoriaApi.Api

Note: the repository does not currently execute migrations automatically on startup.

## Email testing

For local email testing use Mailpit or MailHog and configure `Smtp:Host` and `Smtp:Port` accordingly. Alternatively, point to a real SMTP server.

## Code style and linters

- Use the existing .editorconfig to keep coding conventions consistent.
- Consider using `dotnet format` and Roslyn analyzers for CI checks.

## Recommended tests to add

- Unit tests for Application services (AuthService, InspectionService). Mock IAppDbContext, IFileStorage and IEmailSender.
- Integration tests for database flows using a test container (Docker) for PostgreSQL.
