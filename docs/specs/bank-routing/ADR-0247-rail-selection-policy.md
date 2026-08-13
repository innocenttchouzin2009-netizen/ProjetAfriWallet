# ADR-0247 — Rail Selection Policy

## Status

Accepted

## Context

The routing engine must be deterministic and explainable when several rails are eligible.

## Decision

Ranking uses a fixed sequence: active and healthy rails, then eligibility by country and currency, then amount boundaries, then priority, then cost. SEPA Instant is preferred when it is eligible and healthy for DE/EUR scenarios.

## Consequences

- decisions remain stable across repeated evaluations
- fallback order is explicit and inspectable
- policy changes remain isolated and reviewable
