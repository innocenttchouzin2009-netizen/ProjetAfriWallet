# ADR-0277

## Refund & Chargeback Decision Engine

### Status

Accepted for AFW-DLV-0018.4.

### Context

AFW-DLV-0018.3 produces evidence-based investigation outcomes. A separate bounded context is required to translate those outcomes into dispute-resolution decisions.

### Decision

Create an independent deterministic Decision Engine. The engine consumes immutable investigation snapshots. It does not mutate the investigation.

### Explainability

Every decision records:

- decision type
- reason code
- policy version
- decision factors
- investigation reference
- claim reference
- timestamps

### Idempotence

Repeated evaluation of the same investigation returns the active decision rather than producing duplicates.

### Reevaluation

Explicit reevaluation:

1. supersedes the current decision;
2. preserves historical evidence;
3. produces a new decision identifier.

### Manual approval

High-value or uncertain cases may require manual approval. Approval changes decision state only. It does not execute a refund or chargeback.

### Financial boundary

No operation in AFW-DLV-0018.4 may:

- move money;
- mutate wallet balances;
- write financial ledger entries;
- execute refunds;
- execute chargebacks.

Those capabilities belong to later orchestration/execution layers.
