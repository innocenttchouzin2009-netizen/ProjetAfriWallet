# ADR-0256: Banking Release Candidate

## Status
Accepted.

## Context
The Banking Platform has reached a stable Sprint 15 consolidation point. The release candidate must validate the complete platform stack while maintaining a strict sandbox-only production boundary.

## Decision
We create a release-candidate packaging and validation stage dedicated to freezing the existing Sprint 15 scope. This RC validates architecture, packaging, readiness, documentation and evidence generation without enabling production banking connectivity.

## Consequences
- no new business logic is introduced
- release evidence is immutable
- RC packaging is reproducible
- sandbox-only boundary is enforced
