# ADR-0227 Treasury RC Evidence Packaging

## Context

Treasury RC approval depends on complete release evidence and integrity verification.

## Decision

The RC process must generate validation-report.json, validation-report.md, release-notes.md, changelog.md, manifest.json, and checksums.sha256, alongside OpenAPI, ADR, runbook, dashboard, configuration, artifact, DR, and rollback directories.

## Consequences

- A single package can be promoted across environments.
- Artifact integrity can be verified deterministically.
