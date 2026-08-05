# ADR-0153 — Card State Machine

## Status
Accepted

## Context
The card lifecycle needs explicit and enforceable transitions to preserve business rules and avoid invalid operations.

## Decision
We will model the lifecycle as a state machine with explicit allowed transitions between REQUESTED, ISSUED, PENDING_ACTIVATION, ACTIVE, FROZEN, SUSPENDED, EXPIRED, REPLACED and CLOSED.

## Consequences
- Invalid state changes are rejected explicitly.
- Event generation and timeline integration become deterministic.
- Operators can reason about future state changes without ambiguous behavior.
