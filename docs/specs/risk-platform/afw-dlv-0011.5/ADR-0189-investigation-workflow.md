# ADR-0189 — Investigation Workflow

## Status
Accepted

## Workflow
OPEN -> UNDER_REVIEW -> ESCALATED -> RESOLVED -> CLOSED

## Decision
Use service-enforced transitions driven by assignment, escalation, and decision operations:
- Case creation registers alerts and starts triage.
- Automatic assignment moves case to UNDER_REVIEW.
- Manual assignment can update owner while remaining in review.
- Escalation moves case to ESCALATED with investigator note.
- Decision records disposition and moves case to RESOLVED.
- Explicit closure marks case CLOSED.

## Audit and Telemetry
Each operation appends audit events and updates telemetry derived from alert, evidence, note, and decision counts.

## Consequences
- Traceable investigator actions.
- Predictable behavior for regulatory evidence chains.
- Simple policy evolution through service-level rules.
