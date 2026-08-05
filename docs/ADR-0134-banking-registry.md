# ADR-0134: Banking Registry

## Status
Accepted

## Context
AfriWallet needs a centralized registry for banking providers so that transfer routing decisions can be made deterministically and reused by future connectors such as SEPA, SWIFT, and domestic bank integrations.

## Decision
We will introduce a banking registry module with a domain model for providers, a repository abstraction, an application-layer registry service, and a routing service. The registry will expose deterministic lookup and routing capabilities backed by seed data for development scenarios.

## Consequences
- Future banking connectors can rely on a common registry instead of hard-coded routing logic.
- Routing decisions become testable and environment-aware.
- The module can evolve toward an enterprise payment gateway foundation.
