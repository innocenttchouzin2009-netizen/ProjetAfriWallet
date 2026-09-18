# PRD — AFW-DLV-0012.4

## Summary
Build an internal Reporting & Business Intelligence Platform to consolidate AfriWallet operational data into dashboards, metrics, and exportable reports without direct transactional database reads from the UI.

## Scope
- Executive dashboard for cross-domain reporting.
- Payment, merchant, and support analytics.
- Reporting report creation and listing foundation.
- Export foundation for downstream reporting artifacts.
- Sensitive data protection and operational observability.

## Validation Targets
- dotnet build backend/src/ReportingPlatform/Reporting.Api/Reporting.Api.csproj -c Release
- dotnet run --project backend/tests/Reporting.Scenarios/Reporting.Scenarios.csproj
