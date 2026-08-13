# ADR-0249 — Execution Idempotency and Recovery

## Status

Accepted.

## Context

Bank transfer execution must not create duplicate provider submissions when the same business intent is retried.

## Decision

The execution platform stores idempotency keys and rejects duplicate execution requests for the same logical transfer. Recovery remains conservative and uses explicit failure classification rather than silent retries.

## Consequences

The platform provides a safe base for operational retries while preserving auditability and avoiding duplicate money movement attempts.
