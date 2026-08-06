# Administration Guide — AFW-DLV-0011.5

## Service Overview
The Compliance module orchestrates case creation, assignment, escalation, evidence management, and decision tracking.

## API Operations
- Create case: POST /api/v1/compliance/cases
- List cases: GET /api/v1/compliance/cases
- Get case: GET /api/v1/compliance/cases/{caseId}
- Update case: PUT /api/v1/compliance/cases/{caseId}
- Assign case: POST /api/v1/compliance/cases/{caseId}/assign
- Add evidence: POST /api/v1/compliance/cases/{caseId}/evidence
- Add decision: POST /api/v1/compliance/cases/{caseId}/decision

## Monitoring
Track:
- Status transition distribution
- Escalation rate
- Decision lead time
- Evidence and note density per case

## Hardening Backlog
- Replace in-memory repository with persistent storage.
- Add concurrency guards for multi-operator updates.
- Implement retention and archival controls.
