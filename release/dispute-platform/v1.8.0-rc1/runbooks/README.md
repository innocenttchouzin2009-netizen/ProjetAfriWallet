# Runbooks

This RC ships operational runbooks for the sandbox Dispute Platform.

- Evaluate a dispute decision: `POST /api/v1/disputes/decisions/evaluate` (AFW-DLV-0018.4).
- Orchestrate a resolution: `POST /api/v1/disputes/resolutions` then `/dispatch` (AFW-DLV-0018.5).
- Evaluate customer-protection intelligence: `POST /api/v1/disputes/intelligence/evaluate` (AFW-DLV-0018.6).

All endpoints operate against sandbox infrastructure only. No runbook in this RC authorizes real refund execution, real chargeback submission, or direct Universal Ledger mutation.
