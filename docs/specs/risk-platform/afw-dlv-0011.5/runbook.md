# Runbook — AFW-DLV-0011.5

## Validation Commands
- dotnet run --project backend/tests/Compliance.Scenarios/Compliance.Scenarios.csproj
- dotnet build backend/src/RiskPlatform/Compliance.Api/Compliance.Api.csproj -c Release

## Incident Handling
1. If case creation fails, verify contract payload shape.
2. If assignment fails, validate source and investigator values.
3. If escalation or decision transitions fail, inspect case status and audit events.
4. If telemetry is missing, inspect response mapping in CaseManagementService.

## Rollback Strategy
- Revert to previous known-good branch commit.
- Re-run scenario harness and API build before redeploy.
