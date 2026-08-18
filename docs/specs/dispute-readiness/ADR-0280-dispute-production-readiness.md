# ADR-0280

## Dispute Platform Production Readiness

### Status

Accepted for AFW-DLV-0018.7.

### Context

Sprint 18 now contains six functional dispute-platform deliveries. A transversal gate is required before packaging the platform as a release candidate.

### Decision

Introduce a non-business readiness delivery. AFW-DLV-0018.7 adds no new dispute decision or execution capability.

### Required evidence

The gate must prove:

1. 0018.1 through 0018.6 are frozen.
2. Local and remote tag SHAs match.
3. Tagged commits are contained in origin/main.
4. Platform boundaries remain separated.
5. Audit capabilities exist.
6. Direct real-money execution remains absent.
7. Direct Universal Ledger mutation remains absent.
8. Secret scanning passes.

### Release semantics

A PASS authorizes preparation of AFW-DLV-0018.8. It does not authorize real financial execution.
