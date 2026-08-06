# SLA Runbook — AFW-DLV-0012.2

## Validation Commands
- dotnet build backend/src/SupportPlatform/Support.Api/Support.Api.csproj -c Release
- dotnet run --project backend/tests/Support.Scenarios/Support.Scenarios.csproj

## Operational Triage
1. If warning frequency spikes, inspect assignment delay and queue saturation.
2. If first response breaches increase, verify staffing by category.
3. If resolution breaches increase, escalate unresolved partner dependencies to L3.
