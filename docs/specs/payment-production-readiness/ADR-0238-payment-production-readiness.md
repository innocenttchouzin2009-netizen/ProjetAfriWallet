# ADR-0238 - Payment Production Readiness

## Status

Accepted for AFW-DLV-0014.7.

## Context

Sprint 14 introduced six independently delivered payment capabilities. A release
candidate decision cannot be inferred from file presence or hard-coded PASS
messages; it requires repeatable evidence from the integrated mainline.

## Decision

Introduce an evidence-driven readiness executable and orchestration script.

The script runs real scenario projects, Release builds, secret scanning, and
dependency scanning. Each step writes an isolated log under the ignored
`build/payment-readiness-evidence` directory. The validator consumes those logs
plus repository and package evidence to produce exactly 22 checks.

Missing files, missing markers, failed commands, skipped evidence, manifest
mismatches, and checksum mismatches make the readiness decision fail closed.

## Consequences

- The readiness executable cannot manufacture success without evidence.
- Evidence logs are ephemeral and are not release artifacts.
- The release package stores the deterministic summary, runbooks, configuration,
  dashboard specification, manifest, and checksums.
- Provider certification remains outside this decision.