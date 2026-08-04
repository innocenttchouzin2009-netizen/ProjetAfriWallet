# ADR-0123 — Production Activation Guard

## Status
Accepted

## Context
Production activation must not happen implicitly when secrets are present or when sandbox is active.

## Decision
Production mode requires the explicit `MtnMomo.Production.Enabled` flag and the master flag `MtnMomo.Enabled`. Sandbox and production must not be enabled simultaneously without explicit approval.

## Consequences
- Production cannot be enabled accidentally.
- The platform remains safer for staged rollout.
- Auditable control is preserved for critical operations.
