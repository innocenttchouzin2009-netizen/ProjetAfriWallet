# PRD — AFW-DLV-0011.7

## Summary
Validate in a reproducible way that AFW-DLV-0011.1 to AFW-DLV-0011.6 operate together without regression and meet enterprise production-readiness expectations.

## Goals
- Deliver a deterministic validation gate for Risk, Fraud, and Compliance components.
- Produce CI-consumable JSON and reviewer-friendly Markdown validation reports.
- Generate a release candidate package with operational documentation, OpenAPI, and checksums.

## Scope
- Functional validation of Fraud Engine, AML Monitoring, Risk Scoring, Device Intelligence, Compliance Cases, and Regulatory Reporting.
- Operational controls: configuration, health checks, logging/correlation, resilience, rate limiting, feature flags, OpenTelemetry, metrics, and audit trail.
- Security controls: repository secret scan and sensitive-data exposure checks.
- Release packaging under release/risk-platform/v1.1.0.

## Out of Scope
- New business features for risk decisions.
- Jurisdiction-specific regulatory forms.

## Validation Targets
- powershell -NoProfile -ExecutionPolicy Bypass -File validate-risk-platform.ps1 -Configuration Release
- dotnet build backend/src/RiskPlatform/RiskPlatform.Production/RiskPlatform.Production.csproj -c Release

## Acceptance
- 18 checks executed, 18 passed, 0 failed.
- No real secrets detected.
- Complete release package produced for PR and squash merge workflow.
