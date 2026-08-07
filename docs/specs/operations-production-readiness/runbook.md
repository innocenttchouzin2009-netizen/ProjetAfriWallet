# Runbook

## Commands

- dotnet build backend/src/OperationsPlatform/Production/Operations.Production.csproj -c Release
- dotnet run --project backend/tests/Operations.Readiness.Scenarios/Operations.Readiness.Scenarios.csproj
- powershell -NoProfile -ExecutionPolicy Bypass -File validate-operations-platform.ps1
