# ADR-0029 — Balance Projection

Status: Accepted

## Context

AfriWallet must expose fast wallet balances while preserving ledger integrity as the only financial truth.

## Decision

Displayed balances are projections derived from ledger entries. Projections and snapshots are optimization artifacts and can be fully rebuilt from the ledger.

## Consequences

- Balance Engine does not own financial truth.
- Losing snapshot/projection data does not lose accounting integrity.
- ProjectionVersion tracks synchronization between ledger position and balance projection state.
