# ADR-0201 — Back Office Authorization Model

## Status
Accepted

## Context
Back-office actions have different risk levels and require granular permissions, MFA for sensitive roles, device trust for privileged operations, and immutable audit traces.

## Decision
Model authorization as role-based permissions with action-level checks and explicit strong-confirmation requirements for sensitive operations.

## Consequences
- Positive: permissions can be managed independently of UI structure.
- Positive: sensitive operations are consistently gated.
- Trade-off: new actions must be registered explicitly.
