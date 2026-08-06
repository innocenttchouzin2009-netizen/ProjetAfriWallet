# PRD — AFW-DLV-0011.3

## Summary
Provide a unified risk scoring engine for AfriWallet that aggregates fraud, AML, device, account, beneficiary, KYC, geography, behavior, and payment-type signals into an explainable score and decision.

## Goals
- Produce a single risk evaluation result with a decision, risk band, score, factor contributions, audit events, and telemetry.
- Support explainable scoring for manual review and escalation workflows.
- Validate the engine through executable scenarios covering allow, challenge, review, and block outcomes.

## Scope
- REST endpoint: POST /api/v1/risk/evaluate
- Response includes decision, score, factors, audit events, and telemetry
- Scenario validation harness under backend/tests/RiskScoring.Scenarios

## Validation Evidence
- Scenario harness passed with the following outputs:
  - fraud signal aggregation PASS
  - aml signal aggregation PASS
  - weighted scoring PASS
  - allow decision PASS
  - challenge decision PASS
  - manual review decision PASS
  - block decision PASS
  - audit generation PASS
  - telemetry generation PASS
- Build verification: dotnet build backend/src/RiskPlatform/RiskScoring.Api/RiskScoring.Api.csproj -c Release
