# PRD — AFW-DLV-0012.2

## Summary
Build a Customer Support Case Management Platform for AfriWallet to handle customer requests, incidents, disputes, and operational escalations end to end.

## Scope
- SupportCase domain model and lifecycle.
- Assignment and reassignment workflows.
- SLA policy, warning, breach, and escalation handling.
- Separation between customer messages and internal notes.
- Attachment validation (type and size).
- Timeline, audit events, and telemetry metrics.
- Integration with AFW-DLV-0012.1 Notification Platform.

## Out Of Scope
- Fraud, AML, and compliance investigations (handled by Compliance Case Management).

## Validation Targets
- dotnet build backend/src/SupportPlatform/Support.Api/Support.Api.csproj -c Release
- dotnet run --project backend/tests/Support.Scenarios/Support.Scenarios.csproj
