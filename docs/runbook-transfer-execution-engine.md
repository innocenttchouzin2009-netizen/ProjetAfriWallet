# Runbook — Transfer Execution Engine

## Start
- dotnet run --project backend/src/PaymentGateway/PaymentGateway.Api/PaymentGateway.Api.csproj -c Release

## Health check
- curl http://127.0.0.1:5070/health

## Scenario validation
- dotnet run --project backend/tests/TransferExecution.Scenarios/TransferExecution.Scenarios.csproj

## Troubleshooting
- If the API process is already running, stop it before rebuilding.
- Ensure the base URL matches the running host and port.
