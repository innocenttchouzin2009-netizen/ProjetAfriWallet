# ADR-0088 — Subscription Lifecycle Idempotency

## Status
Accepted

## Context
Repeated creation requests for the same user and provider should not create duplicate active subscriptions.

## Decision
We will enforce idempotency at creation time by returning the existing subscription when a matching user-provider subscription already exists.

## Consequences
- Client retries become safe and predictable
- Duplicate subscriptions are avoided without extra reconciliation work
- The lifecycle engine remains resilient under transient client failures
