# AFW-DLV-0007.4.6 Validation Report

## Build
- dotnet build backend/src/PaymentGateway/PaymentGateway.Api/PaymentGateway.Api.csproj -c Release

## Scenario run
- dotnet run --project backend/tests/TransferExecution.Scenarios/TransferExecution.Scenarios.csproj

## Expected outcome
- Execution queued PASS
- Connector resolution PASS
- Dispatch PASS
- Retry policy PASS
- Timeout handling PASS
- Settlement PASS
- Completion PASS
- Rollback PASS
- Audit events PASS
- Telemetry PASS
- Recovery after restart PASS
