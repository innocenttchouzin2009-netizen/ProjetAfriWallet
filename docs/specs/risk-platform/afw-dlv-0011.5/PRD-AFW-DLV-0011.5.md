# PRD — AFW-DLV-0011.5

## Summary
Implement a Compliance Case Management System that centralizes alerts from Fraud Detection, AML Monitoring, Risk Scoring, and Device Intelligence into an auditable investigation workflow.

## Goals
- Open compliance cases from multiple automatic and manual sources.
- Support investigator assignment, evidence collection, escalation, resolution, and closure.
- Maintain an auditable lifecycle: OPEN, UNDER_REVIEW, ESCALATED, RESOLVED, CLOSED.
- Emit telemetry and audit events for each case transition.

## In Scope
- Case lifecycle service and REST API.
- Automatic assignment based on alert source.
- Manual reassignment by compliance operators.
- Evidence and investigator note management.
- Decision recording and case closure.

## Out of Scope
- External persistence adapters beyond in-memory storage.
- Workflow UI and human-task orchestration frontend.
- Machine-learning based risk model training.

## API Scope
- POST /api/v1/compliance/cases
- GET /api/v1/compliance/cases
- GET /api/v1/compliance/cases/{caseId}
- PUT /api/v1/compliance/cases/{caseId}
- POST /api/v1/compliance/cases/{caseId}/assign
- POST /api/v1/compliance/cases/{caseId}/evidence
- POST /api/v1/compliance/cases/{caseId}/decision

## Acceptance Criteria
- Compliance scenarios print all expected PASS lines.
- Compliance API builds in Release mode.
- Documentation package is complete under docs/specs/risk-platform/afw-dlv-0011.5.
