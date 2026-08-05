# ADR-0141 — Connector Resolution Strategy

## Status
Accepted

## Context
The execution engine needs to select the right connector automatically based on the provider and transfer type without leaking provider-specific concerns to business modules.

## Decision
Use a resolver that maps supported provider codes to connector types and execution modes. The strategy is intentionally simple and deterministic for the current release, with room to evolve into richer routing rules later.

## Consequences
- Connector selection is centralized.
- The engine remains provider-agnostic at the business layer.
- Additional routing rules can be introduced as the gateway grows.
