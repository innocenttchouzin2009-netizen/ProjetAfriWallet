# AFW-DLV-0009.1 — Merchant Registry PRD

## Summary
Deliver the initial Merchant Registry for Sprint 9 so AfriWallet can register, validate, and manage merchants as first-class entities. The release establishes the architectural foundation for downstream onboarding, QR payments, settlement, and merchant APIs.

## Objectives
- Provide a merchant aggregate with business identity, status, capabilities, and wallet linkage.
- Implement merchant registration and lifecycle operations for activation, suspension, and closure.
- Validate merchant data using country, currency, and identity rules.
- Expose merchant endpoints through the Merchant API.

## Scope
### In scope
- Merchant creation and lookup.
- Merchant update and lifecycle transitions.
- Duplicate merchant rejection and basic validation rules.
- Audit and telemetry hooks for merchant operations.
- QR payment and settlement scaffolding endpoints.

### Out of scope
- Full merchant onboarding and KYC workflows.
- Production-grade persistence and external integrations.
- Complete settlement execution and reconciliation.

## Acceptance criteria
- The Merchant API builds successfully in Release mode.
- Merchant registry scenarios pass for create, update, validation, activation, suspension, closure, audit, and telemetry.
- Merchant, QR payment, and settlement endpoints compile without unresolved type ambiguity.
