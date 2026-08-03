# ADR-0027 — Immutable Ledger

Status: Accepted

## Context

AfriWallet needs a financial ledger that remains auditable and tamper-resistant after posting.

## Decision

A ledger entry is immutable after creation. No update or delete operation is allowed for posted entries. Any correction must be expressed as a new compensating or reversal entry.

## Consequences

- Every financial event remains traceable.
- Audit and reconciliation remain consistent over time.
- Incorrect postings are corrected through explicit compensating transactions.
