# ADR-0208 - Operations Production Readiness Gate

## Context

Operations capabilities are implemented across multiple deliveries and require a formal readiness gate before RC publication.

## Decision

Introduce AFW-DLV-0012.7 as a production-readiness delivery with deterministic checks, release evidence, and explicit pass/fail criteria.

## Consequences

- Releases become auditable and repeatable
- Readiness status is clear before RC tagging
- CI governance aligns with release policy
