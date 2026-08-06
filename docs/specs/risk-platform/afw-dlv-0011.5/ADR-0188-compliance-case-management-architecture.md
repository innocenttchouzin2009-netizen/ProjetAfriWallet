# ADR-0188 — Compliance Case Management Architecture

## Status
Accepted

## Context
RiskPlatform now produces alerts across Fraud, AML, Risk Scoring, and Device Intelligence. Compliance operations require one unified case object and workflow for investigation traceability.

## Decision
Adopt a layered module under RiskPlatform with Domain, Contracts, Application, Infrastructure, and API projects. Implement a deterministic workflow service with explicit state transitions and audit events.

## Rationale
- Keeps consistency with existing RiskPlatform modules.
- Enables explainable operations and deterministic scenario validation.
- Separates API contracts from domain entities.

## Consequences
- Fast delivery and easy testing through in-memory orchestration.
- Clear extension point for persistent storage in Compliance.Infrastructure.
- Requires future hardening for distributed concurrency and retention policies.
