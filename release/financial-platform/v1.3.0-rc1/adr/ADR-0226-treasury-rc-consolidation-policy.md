# ADR-0226 Treasury RC Consolidation Policy

## Context

Sprint 13 requires a release candidate that consolidates pre-validated treasury deliveries.

## Decision

The RC gate validates evidence from AFW-DLV-0013.1 through AFW-DLV-0013.7 and packages those artifacts under release/financial-platform/v1.3.0-rc1.

## Consequences

- Release readiness becomes reproducible and auditable.
- No new domain behavior is introduced at RC stage.
