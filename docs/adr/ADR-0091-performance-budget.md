# ADR-0091: Performance Budget

## Status
Accepted

## Context
AfriWallet must preserve predictable latency and availability under higher traffic and partial saturation.

## Decision
The platform defines a performance budget with an availability target above 99.9%, P95 below 250 ms, P99 below 500 ms, and error rate below 0.1%.

## Consequences
Performance regressions can be tracked against explicit budgets during release validation.
