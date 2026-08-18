# ADR-0279

## Customer Protection & Dispute Intelligence Engine

### Status

Accepted for AFW-DLV-0018.6.

### Context

Individual disputes provide useful operational information, but customer-protection risks often appear across multiple claims, merchants, beneficiaries and resolution workflows.

### Decision

Introduce a dedicated deterministic intelligence bounded context. It consumes normalized snapshots and does not mutate the previous Sprint 18 engines.

### Explainability

Every protection finding contains:

- score
- severity
- recommendation
- metrics
- deterministic patterns
- references
- human-readable reasons

### Determinism

No opaque machine-learning classifier is introduced.

### Enforcement boundary

A `ReviewMerchant` result does not block a merchant. An `EscalateOperations` result does not suspend a customer. All enforcement remains outside this delivery.
