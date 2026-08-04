# ADR-0131 — Release Evidence Preservation

## Status
Accepted

## Context
Release validation results must be preserved as reviewable evidence for QA, security, and operations teams. The evidence package should remain deterministic and easy to inspect in GitHub or CI logs.

## Decision
Validation reports, configuration snapshots, runbooks, dashboards, and SHA-256 manifests will be stored under the release evidence bundle.

## Consequences
- Every release candidate has a transparent evidence package.
- Integrators can verify artifact integrity with the generated checksum manifest.
