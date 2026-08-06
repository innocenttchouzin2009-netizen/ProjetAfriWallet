# ADR-0194 — Risk Platform Release Candidate Process

## Status
Accepted

## Context
Sprint 11 closes with a release candidate freeze that must consolidate six business modules plus production-readiness controls into a single reproducible package.

## Decision
Adopt a release candidate process that:
- Uses a dedicated branch `release/risk-platform-v1.1.0-rc1`.
- Validates all Sprint 11 modules and readiness controls through one script.
- Produces versioned release package artifacts and checksums.
- Requires Squash and Merge before any tag creation.

## Consequences
- Positive: clear freeze point for RC governance.
- Positive: reproducible evidence bundle for review and audit.
- Trade-off: release candidate promotion now depends on a broader gate.
