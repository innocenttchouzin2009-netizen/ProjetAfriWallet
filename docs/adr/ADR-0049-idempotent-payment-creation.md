# ADR-0049: Idempotent Payment Creation

## Status
Accepted

## Context
Payment creation must remain safe after transport retries or mobile reconnects.

## Decision
Every Payment Intent creation requires an Idempotency-Key and the backend will return the original intent when the same key is re-used with an identical payload, and reject conflicts when the payload changes.

## Consequences
The API prevents duplicate payment intents and preserves a single source of truth for each client request.
