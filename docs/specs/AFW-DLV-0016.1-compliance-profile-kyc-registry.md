# AFW-DLV-0016.1 — Compliance Profile & KYC Registry

## Objective
Create the first compliance platform module for KYC profile capture, document registration, and bounded review workflows in a sandbox-only design.

## In Scope
- Domain model for KYC profile lifecycle
- Repository and in-memory persistence
- Application service for create, retrieve, document add, review, and suspension flows
- API endpoints for profile management
- Audit sink for traceability
- Scenario coverage to validate the sandbox flow
- CI workflow and release helper script

## Out of Scope
- Real sanctions provider integration
- Real document verification vendor integration
- Production KYC decisioning logic outside the sandbox contract layer

## Acceptance Criteria
1. A compliance profile can be created for a customer.
2. Documents can be attached to a profile.
3. A profile can be approved or rejected with rationale.
4. A profile can be suspended for manual review or compliance action.
5. Audit history is retained for every transition.
6. The module remains provider-neutral and sandbox-safe.
