# ADR-0190 — Regulatory Reporting Architecture

## Status
Accepted

## Context
RiskPlatform produces multiple classes of risk and compliance signals that must be represented in a coherent reporting workflow for regulators.

## Decision
Introduce a dedicated RegulatoryReporting module with Domain, Application, Contracts, Infrastructure, and API layers. Keep in-memory orchestration for this delivery while preserving extensibility for persistence and external signing providers.

## Consequences
- Deterministic lifecycle control and scenario validation.
- Separation of workflow orchestration from transport contracts.
- Ready extension point for authority-specific adapters.
