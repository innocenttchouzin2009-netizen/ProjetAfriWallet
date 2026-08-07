# ADR-0209 - Release Validation Strategy for Operations

## Context

Release decisions require objective evidence beyond feature completion.

## Decision

Use a dedicated validator and scenario runner as mandatory release gates for Operations. Generate machine-readable and human-readable reports, plus checksums.

## Consequences

- Validation can be automated in CI/CD
- Evidence can be archived for compliance and audit
- Failures are isolated before RC promotion
