FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY VistoriaApi.sln ./
COPY src/VistoriaApi.Domain/VistoriaApi.Domain.csproj src/VistoriaApi.Domain/
COPY src/VistoriaApi.Application/VistoriaApi.Application.csproj src/VistoriaApi.Application/
COPY src/VistoriaApi.Infrastructure/VistoriaApi.Infrastructure.csproj src/VistoriaApi.Infrastructure/
COPY src/VistoriaApi.Api/VistoriaApi.Api.csproj src/VistoriaApi.Api/

RUN dotnet restore VistoriaApi.sln

COPY src ./src

RUN dotnet publish src/VistoriaApi.Api/VistoriaApi.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .

RUN mkdir -p /app/uploads && chown -R app:app /app

USER $APP_UID

ENTRYPOINT ["dotnet", "VistoriaApi.Api.dll"]
