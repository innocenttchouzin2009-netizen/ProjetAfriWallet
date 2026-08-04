# ADR-0031: Resilience and failure tolerance for production operations

## Status
Accepted

## Context
AFW-0005.8.3 requires a production-ready resilience layer for backend operations that must tolerate transient failures, avoid duplicate processing, and preserve recovery semantics under partial outages.

## Decision
We introduce a small resilience module with explicit timeout policies, retry with backoff, circuit breakers, bulkhead isolation, idempotent execution, DLQ handling, replay, outbox and inbox tracking, and a minimal API surface.

## Consequences
- Critical flows can be bounded and retried safely.
- Duplicate processing is reduced through an idempotency store.
- Failed messages can be isolated and replayed without losing the original intent.
- The module is intentionally lightweight and in-memory for the current implementation so it can be verified with scenario tests before deeper persistence integration.
