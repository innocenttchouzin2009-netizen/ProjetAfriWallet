# ADR-0193 — Risk Operational Validation Strategy

## Status
Accepted

## Context
Operational incidents often come from configuration drift, missing observability controls, and inconsistent resilience behavior. Risk workloads require deterministic validation before release candidates.

## Decision
Adopt a layered validation strategy:
1. Static controls: config, appsettings hygiene, secret scanning, protected internal endpoints.
2. Runtime controls: health endpoints, rate limiting, metrics and telemetry availability, audit trail protections.
3. Functional controls: scenario execution for fraud, AML, risk scoring, device, compliance, and regulatory reporting.
4. Release controls: production build, release package generation, SHA-256 manifests.

## Consequences
- Positive: broad operational coverage with one execution command.
- Positive: CI integration through machine-readable JSON reports.
- Trade-off: validation script ownership becomes a critical maintenance responsibility.
