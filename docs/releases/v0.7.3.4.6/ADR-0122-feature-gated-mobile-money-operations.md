# ADR-0122 — Feature-Gated Mobile Money Operations

## Status
Accepted

## Context
MTN MoMo capabilities must be activated progressively and securely to avoid premature exposure of sensitive flows and to support staged rollout.

## Decision
Feature flags will control whether the MobileMoney API exposes MTN MoMo operations and related capabilities. Production activation remains disabled by default and requires an explicit flag.

## Consequences
- Progressive rollout becomes possible without redeployment.
- Sensitive operations can be disabled safely.
- Diagnostics remain safe and do not expose secrets.
