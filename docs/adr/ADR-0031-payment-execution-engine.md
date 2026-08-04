# ADR-0031: Payment Execution Engine

## Status
Accepted

## Context
AFriWallet already had:
- wallet identity and lifecycle management;
- an immutable ledger and balance projection model;
- payment intents;
- validation and authorization steps.

What was missing was the first real financial movement: executing an authorized payment intent as a complete transfer between wallets.

## Decision
We introduce a payment execution engine that:
1. Consumes a valid authorization for a payment intent.
2. Consumes the funds reservation tied to the intent.
3. Posts a double-entry ledger transaction from the source wallet to the destination wallet.
4. Rebuilds the affected balance projections after posting.
5. Preserves idempotence by reusing the same execution outcome for repeated calls on the same intent.

The execution flow is intentionally orchestration-focused and consumes the existing ledger, reservation, and projection services rather than introducing a parallel financial subsystem.

## Consequences
### Positive
- The first wallet-to-wallet money movement is now implemented in the core domain model.
- Ledger entries are created through the existing accounting layer, preserving double-entry consistency.
- Reservations are consumed only once, and repeated execution requests remain safe.
- Balance projections are refreshed after a successful transfer.

### Negative
- The execution path is currently in-memory and does not yet cover downstream event publishing, reconciliation, or external settlement integration.
- Real-world retries and persistence semantics will need further hardening later.
