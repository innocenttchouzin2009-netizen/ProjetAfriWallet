# ADR-0149 — Real-Time Authorization Decision Model

## Status
Accepted

## Context
The authorization engine needs a stable decision vocabulary that can be consumed by gateways, operators and downstream analytics while preserving traceability.

## Decision
We will use a compact decision vocabulary: AUTHORIZED, DECLINED, INSUFFICIENT_FUNDS, CARD_FROZEN, CARD_CLOSED, LIMIT_EXCEEDED, CONTROL_BLOCKED, FRAUD_SUSPECTED and MANUAL_REVIEW. Each authorization carries a reason code, trace id and correlation id.

## Consequences
- Decision handling becomes consistent across services and tests.
- Operators can quickly understand why a transaction was routed to a specific outcome.
- The model is extensible for future payment network integrations.
