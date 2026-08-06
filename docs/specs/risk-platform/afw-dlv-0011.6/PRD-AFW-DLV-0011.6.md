# PRD — AFW-DLV-0011.6

## Summary
Deliver a jurisdiction-agnostic regulatory reporting engine that aggregates references and snapshots from Fraud Detection, AML Monitoring, Risk Scoring, Device Intelligence, and Compliance Case Management.

## Goals
- Generate traceable, versioned, signed-ready, exportable reports.
- Enforce strict lifecycle transitions with invalid transition rejection.
- Keep source data lightweight through references and controlled snapshots.

## Scope
- Regulatory report domain and lifecycle services.
- Versioning with author, timestamp, reason, checksum, and diff summary.
- Submission tracking and decision flow.
- Exports in JSON, CSV, and PDF payload formats.

## Constraints
- No country-specific official form replication until regulator context is defined.
- No private cryptographic keys in repository.

## Validation Targets
- Build: backend/src/RiskPlatform/RegulatoryReporting.Api/RegulatoryReporting.Api.csproj -c Release
- Scenarios: backend/tests/RegulatoryReporting.Scenarios/RegulatoryReporting.Scenarios.csproj
