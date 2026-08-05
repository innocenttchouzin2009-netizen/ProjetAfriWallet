# ADR-0152 — Card Lifecycle Management

## Status
Accepted

## Context
AfriWallet needs a single entry point for all card lifecycle operations so wallet, merchant and support flows cannot bypass lifecycle controls.

## Decision
We will implement a CardLifecycleService that owns issuance, activation, freeze, unfreeze, suspension, resume, replacement, expiry and closure transitions.

## Consequences
- Card lifecycle operations become consistent and auditable.
- Consumers interact through a single lifecycle manager rather than manipulating cards directly.
- The lifecycle service can be reused by wallet, payments and support integrations.
