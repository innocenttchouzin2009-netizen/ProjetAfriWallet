# Exception Management Guide

Exception handling policy:
- Keep newly generated exceptions in Open status.
- Investigate source-system gaps first.
- Resolve only when corrective evidence exists.
- Keep exception IDs in audit logs and incident tickets.

Primary codes:
- EXTERNAL_RECORD_MISSING
- INTERNAL_RECORD_MISSING
- RECONCILIATION_DIFFERENCE
