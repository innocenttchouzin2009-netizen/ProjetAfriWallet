# AfriWallet Merchant Platform v1.9.0-rc1

Delivery: AFW-DLV-0019.8 — Merchant Platform Release Candidate

## Scope
This release candidate consolidates the frozen Sprint 19 Merchant Platform deliveries 0019.1 through 0019.7.

## Included capabilities
- Merchant registry and business profiles
- Merchant onboarding and verification orchestration
- Checkout sessions and payment intents
- Merchant payment decision capabilities delivered in Sprint 19
- Merchant settlement capabilities delivered in Sprint 19
- Merchant risk, commerce intelligence and protection
- Merchant platform production-readiness gates

## Frozen prerequisite
AFW-DLV-0019.7 is frozen at `3b0fa213c9d25b680ad16914278934bd8e6971c4` with annotated tag `sprint19-dlv-0019.7`.

## Non-execution boundaries
This RC does not introduce automatic merchant blocking or suspension, automatic settlement/payout freeze, payment capture, money movement, or ledger mutation beyond the boundaries of the already frozen deliveries.

## Release protocol
The RC tag must only be created after PR CI succeeds, the PR is squash-merged, the authoritative squash SHA is verified on `main`, and local/remote peeled tag parity is verified.
