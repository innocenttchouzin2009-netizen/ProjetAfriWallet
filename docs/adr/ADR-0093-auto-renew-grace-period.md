# ADR-0093 — Auto-Renew Grace Period

## Status
Accepted

## Context
Renewal attempts may fail repeatedly and should not immediately destroy the subscription relationship.

## Decision
We will move a renewal job to a grace-period state after the retry budget is exhausted, allowing downstream support or recovery workflows to intervene.

## Consequences
- Failed renewals are handled predictably
- Subscription continuity is preserved during transient failure windows
- Operations can investigate the reason without losing the subscription state
