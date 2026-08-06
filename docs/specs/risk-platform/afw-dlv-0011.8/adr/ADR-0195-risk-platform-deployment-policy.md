# ADR-0195 — Risk Platform Deployment Policy

## Status
Accepted

## Context
The Risk Platform now spans fraud, AML, scoring, device, compliance, regulatory reporting, and production-readiness controls. Deployment policy must prevent drift between the release candidate and the committed source.

## Decision
Require:
- Build verification before merge.
- Tagged release only after merge lands on `origin/main`.
- Release artifacts to be treated as immutable evidence.
- Internal operational endpoints to remain protected in production readiness.

## Consequences
- Positive: controlled deployment path for the RC.
- Positive: traceable evidence from source to package.
- Trade-off: slower promotion, but safer release governance.
