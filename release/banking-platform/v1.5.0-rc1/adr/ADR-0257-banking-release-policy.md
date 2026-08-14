# ADR-0257: Banking Release Policy

## Status
Accepted.

## Context
The Banking Platform must only be released after a verified RC gate and exact SHA tag freeze.

## Decision
The release decision is directly tied to the merged squash SHA and the corresponding tagged RC.

## Consequences
- no unverified tag creation
- no production bank traffic
- immutable evidence preserved throughout the release gate
