# ADR-0140 — Unified Transfer Execution Engine

## Status
Accepted

## Context
AfriWallet requires a universal execution engine that can route transfer intents to the appropriate connector and lifecycle them through execution states while preserving idempotence, retries, auditability, and observability.

## Decision
Introduce a self-contained transfer execution engine in the payment gateway layer that handles dispatch, retries, state tracking, and basic observability. The engine exposes REST endpoints for creation, lookup, retry, and cancellation.

## Consequences
- Business modules can submit transfer intents without knowing connector details.
- Execution is straightforward to test and evolve.
- The initial implementation uses in-memory persistence and deterministic connector rules.
