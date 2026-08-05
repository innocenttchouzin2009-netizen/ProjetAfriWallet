# ADR-0133 — Release Artifact Policy

## Status
Accepted

## Context
Release evidence must be preserved in a deterministic form for traceability, auditability, and downstream validation.

## Decision
Every RC package contains a validation report, checksum manifest, architecture decision records, runbooks, dashboards, configuration notes, and OpenAPI assets.

## Consequences
- The package is self-contained and reviewable.
- Consumers can verify integrity and reproducibility.
- The release process becomes auditable.
