# AFW-DLV-0007.4.4 — Beneficiary & Account Verification Engine

## Scope
- Introduce a beneficiary domain model for registered transfer beneficiaries.
- Validate beneficiary-account compatibility for country, currency, and account structure.
- Provide a verification engine that produces a verification outcome and correlation ID.

## Delivered components
- Beneficiary entity and verification record
- Beneficiary repository and validator
- Beneficiary service and verification engine
- API endpoints for beneficiary CRUD and verification
- Scenario project for end-to-end validation

## Verification
- Build: dotnet build backend/src/Banking/Banking.Api/Banking.Api.csproj -c Release
- Scenario: dotnet run --project backend/tests/Banking.Beneficiaries.Scenarios/Banking.Beneficiaries.Scenarios.csproj
