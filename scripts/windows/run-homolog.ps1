$ErrorActionPreference = "Stop"

if (-not (Test-Path ".env.homolog")) {
    throw "Crie o arquivo .env.homolog a partir de .env.homolog.example."
}

docker compose `
    --env-file .env.homolog `
    -f docker-compose.homolog.yml `
    up --detach
