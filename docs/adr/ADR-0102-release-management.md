# ADR-0102: Release Management

- Status: Accepted
- Date: 2026-08-04

## Context
AfriWallet requires a predictable release process for versioning, changelog generation, tagging, and artifact publication.

## Decision
Release automation will use versioned scripts and GitHub release workflow entry points so artifacts, notes, and tags are generated from a controlled process.

## Consequences
The release flow becomes repeatable and easier to audit, while remaining flexible enough to incorporate more sophisticated tooling later.
