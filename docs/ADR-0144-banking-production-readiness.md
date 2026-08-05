# ADR-0144 — Banking Production Readiness

## Status
Accepted

## Context
The banking domain requires a production-ready release path that includes configuration validation, health checks, structured logging, resilience, telemetry, monitoring, auditability, workflow orchestration, and packaging.

## Decision
The banking API will expose a lightweight production-readiness surface built into the existing API entrypoint. This includes health endpoints, telemetry metrics, audit-oriented logging, and a validation script that packages release artifacts.

## Consequences
The banking API becomes deployable in a controlled environment and can be validated locally before release.
