# ADR-0138 — Unified Transfer Intent Architecture

## Status
Accepted

## Context
AfriWallet needs a single, explicit step between a payment request and execution so that providers, banks, and cards can all follow the same lifecycle.

## Decision
Introduce a transfer-intent engine that creates intents first, validates them, reserves funds, transitions them to ready, and only then submits them for execution. The engine will expose REST endpoints for creation, retrieval, listing, cancellation, and confirmation.

## Consequences
- Payments become auditable and idempotent.
- Providers can share a consistent state machine.
- Observability and telemetry can be attached uniformly to each transition.
