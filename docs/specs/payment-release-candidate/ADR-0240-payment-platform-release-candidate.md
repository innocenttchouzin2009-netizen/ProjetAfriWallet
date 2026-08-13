# ADR-0240: Payment Platform Release Candidate

## Status

Accepted.

## Context

The payment platform has completed the required delivery sequence and requires a final release-candidate gate before any production-facing freeze.

## Decision

We will generate a release candidate package that contains the signed release validation evidence, manifest, checksum, and distribution artifacts without adding new business features.

## Consequences

- The package is intentionally validation-only.
- The release is not a production certification.
- The delivery boundary remains sandbox-only for external providers.
