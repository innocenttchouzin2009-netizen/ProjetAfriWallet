# ADR-0087 — Subscription Lifecycle Event Immutability

## Status
Accepted

## Context
State transitions must remain auditable and safe for downstream reconciliation or dispute handling.

## Decision
We will keep an immutable history of state changes for every subscription, recording the transition status, timestamp, and reason.

## Consequences
- Every lifecycle transition is traceable
- Reconciliation and support workflows can inspect historical changes easily
- The record becomes a durable audit trail for operational teams
