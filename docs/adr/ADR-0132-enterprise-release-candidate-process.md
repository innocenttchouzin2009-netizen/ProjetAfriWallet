# ADR-0132 — Enterprise Release Candidate Process

## Status
Accepted

## Context
The MTN MoMo enterprise module must be released as a controlled release candidate for integration testing without implying production connectivity.

## Decision
The release candidate process will use one reproducible build script, one frozen validation report, one checksum manifest, and one release package that is tagged and preserved for review.

## Consequences
- RC quality is transparent and repeatable.
- Test and integration teams receive a stable, reviewable package.
- Production systems remain explicitly out of scope.
