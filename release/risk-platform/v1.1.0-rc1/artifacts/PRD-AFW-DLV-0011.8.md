# PRD — AFW-DLV-0011.8

## Summary
Freeze the Risk, Fraud & Compliance platform and package AFW-DLV-0011.1 through AFW-DLV-0011.7 as a single enterprise release candidate.

## Goals
- Provide a single release candidate validation gate for the full Risk Platform.
- Re-validate the integration surface of all Sprint 11 deliveries.
- Generate a release package that can be attached to the PR and used for Squash and Merge.

## Scope
- Fraud Detection Engine
- AML & Transaction Monitoring
- Unified Risk Scoring Engine
- Device Intelligence & Behavioral Analytics
- Compliance Case Management
- Regulatory Reporting
- Production Readiness controls from AFW-DLV-0011.7
- Release candidate packaging and documentation

## Out of Scope
- New business capabilities.
- Jurisdiction-specific regulatory policy changes.
- Runtime feature expansion beyond freeze scope.

## Validation Targets
- powershell -NoProfile -ExecutionPolicy Bypass -File build-risk-release.ps1 -Configuration Release
- dotnet build backend/src/RiskPlatform/RiskPlatform.Production/RiskPlatform.Production.csproj -c Release

## Acceptance
- 18 checks executed, 18 passed, 0 failed.
- Package release/risk-platform/v1.1.0-rc1 generated.
- RC-ready documentation and checksums produced.
