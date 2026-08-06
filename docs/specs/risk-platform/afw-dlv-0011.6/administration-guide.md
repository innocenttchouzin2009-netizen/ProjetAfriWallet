# Administration Guide — AFW-DLV-0011.6

## Security and Roles
Actions approve, submit, accept, and reject are restricted to privileged roles:
- compliance_manager
- risk_manager
- regulatory_officer
- admin

## Integrity Controls
- SHA-256 checksum is generated per version.
- Signature abstraction uses IRegulatoryReportSigner.
- No private key storage in repository.

## Observability Metrics
- afw_regulatory_reports_created_total
- afw_regulatory_reports_submitted_total
- afw_regulatory_reports_rejected_total
- afw_regulatory_report_generation_duration_ms
- afw_regulatory_report_export_total
- afw_regulatory_report_versions_total

## Hardening Backlog
- Add persistent repository in infrastructure layer.
- Plug HSM/KMS-backed signer implementation.
- Extend authority-specific export adapters.
