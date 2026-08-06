# ADR-0198 — Customer Support Platform Architecture

## Status
Accepted

## Context
AfriWallet support, operations, and compliance teams need a dedicated customer support case platform for client assistance and operational incidents. This platform must remain separated from Compliance Case Management while allowing optional linkage to compliance investigations.

## Decision
Adopt a standalone Support Platform composed of Domain, Application, Contracts, Infrastructure, API, and scenario validation modules.

## Consequences
- Positive: support operations workflows are decoupled from compliance investigations.
- Positive: SLA, assignment, and escalations are managed with support-specific logic.
- Trade-off: cross-platform traceability requires explicit linking through RelatedComplianceCaseId.
