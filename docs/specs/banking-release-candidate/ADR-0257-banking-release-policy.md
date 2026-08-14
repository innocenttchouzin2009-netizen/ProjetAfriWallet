# ADR-0257: Banking Release Policy

## Status
Accepted.

## Context
The Banking Platform can only be released after a verified RC gate, package integrity checks and exact SHA-based tag freeze.

## Decision
The release decision is tied to a squash-merged commit, exact SHA verification and a tagged RC that preserves the merged artifact exactly.

## Consequences
- no tag is created before merge verification
- no production bank traffic is enabled
- all evidence remains tied to the exact release SHA
