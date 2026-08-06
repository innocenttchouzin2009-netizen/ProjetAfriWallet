# Submission Runbook — AFW-DLV-0011.6

## Operational Sequence
1. Generate report.
2. Move to review.
3. Approve with privileged role.
4. Submit with privileged role.
5. Record acceptance or rejection response.
6. Archive accepted reports.

## Failure Handling
- Invalid transition: reject action and keep current status.
- Rejected submission: regenerate report from REJECTED status and create new version.
- Missing export format: return unsupported format error.

## Evidence Trail
Check audit timeline, submission history, version checksums, and export metadata for traceability.
