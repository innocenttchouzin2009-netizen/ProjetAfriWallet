# ADR-0273 - Fraud Platform Release Candidate

## Status

Accepted for AFW-DLV-0017.8.

## Decision

Package Sprint 17 as `v1.7.0-rc1` after verifying seven immutable delivery tags, local/remote parity, and main-history membership.

The final RC tag is created only on the authoritative squash SHA produced by the AFW-DLV-0017.8 PR merge. Historical tags are never moved or recreated.