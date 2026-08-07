# PRD - AFW-DLV-0012.6

## Summary
Build the AfriWallet Production Operations & SRE Platform as an enterprise Operations Center for service health, incidents, alerts, maintenance, deployments, backups, and disaster recovery.

## Scope
- Aggregated service health and uptime.
- Incident lifecycle management.
- Alert inventory and escalation state.
- Maintenance planning and visibility.
- Deployment history and production release tracking.
- Backup inventory and verification status.
- Disaster recovery plan registry.
- Operational metrics and audit trail.

## Validation
- `dotnet build backend/src/OperationsPlatform/Operations.Api/Operations.Api.csproj -c Release`
- `dotnet run --project backend/tests/Operations.Scenarios/Operations.Scenarios.csproj`