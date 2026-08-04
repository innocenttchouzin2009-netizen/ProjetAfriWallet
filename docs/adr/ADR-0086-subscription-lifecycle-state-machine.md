# ADR-0086 — Subscription Lifecycle State Machine

## Status
Accepted

## Context
Subscriptions require explicit lifecycle states that cover initiation, payment confirmation, activation, suspension, grace periods, renewal, cancellation, and expiration.

## Decision
We will model a subscription lifecycle with the following progression:
- DRAFT -> PENDING_PAYMENT -> ACTIVE
- optional suspension/resume transitions
- grace period handling for late renewals
- cancellation and expiration as terminal states for non-renewing subscriptions

## Consequences
- Subscription behavior becomes predictable and observable
- Payment and billing integrations can drive state changes through a simple contract
- The domain is prepared for future automation and reconciliation workflows
