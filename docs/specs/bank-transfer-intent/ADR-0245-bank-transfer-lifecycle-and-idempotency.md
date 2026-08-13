# ADR-0245 — Bank Transfer Lifecycle and Idempotency

## Status

Accepted

## Context

Transfer intents must remain safe under retries and lifecycle transitions. The same request should not create multiple intent records when repeated with the same idempotency key.

## Decision

The service treats idempotency as a repository concern and rejects invalid lifecycle transitions. The object state machine allows only a strict progression from Created to Confirmed to ReadyForRouting, whereas cancellation and expiry remain terminal safety boundaries.

## Consequences

- Clients can retry safely without duplicate transfers.
- Lifecycle mistakes fail fast and remain auditable.
- Later routing and execution layers can rely on a single stable intent record.
