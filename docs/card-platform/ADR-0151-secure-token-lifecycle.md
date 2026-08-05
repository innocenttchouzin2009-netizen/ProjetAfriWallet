# ADR-0151 — Secure Token Lifecycle

## Status
Accepted

## Context
Tokens require lifecycle control to prevent reuse after suspension, revocation or expiration.

## Decision
The token lifecycle will move through REQUESTED, GENERATED, ACTIVE, SUSPENDED, REVOKED, EXPIRED and ROTATED states. Each transition is validated before execution and logged as an audit event.

## Consequences
- Tokens can be safely suspended or revoked when a card is compromised.
- Rotation can be used to minimize token reuse windows.
- Validation logic remains deterministic and testable.
