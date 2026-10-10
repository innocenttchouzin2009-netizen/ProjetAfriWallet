# ADR-0203 — Analytical Data Strategy

## Status
Accepted

## Context
The reporting layer must not query live operational databases for each dashboard request.

## Decision
Rely on precomputed read projections or analytical stores for BI queries, with sandbox adapters only as temporary implementation details during delivery and validation.

## Consequences
- Positive: reporting reads remain decoupled from core transactional domains.
- Positive: metric definitions can be governed and versioned.
- Trade-off: data pipelines are required to keep projections current.
