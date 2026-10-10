# ADR-0030 — Projection Position and Rebuild

Status: Accepted

## Context

Balance projections must remain performant and recoverable at scale while preserving ledger truth.

## Decision

Each wallet projection tracks a strictly increasing LastLedgerPosition. Incremental projection reads ledger records with positions greater than LastLedgerPosition. Full rebuild is always possible by replaying ledger records from position 0.

## Consequences

- Projection freshness can be measured by comparing LastLedgerPosition with current ledger position.
- Projection idempotence is guaranteed by position-based replay.
- Snapshot loss does not compromise correctness because full rebuild remains deterministic.
