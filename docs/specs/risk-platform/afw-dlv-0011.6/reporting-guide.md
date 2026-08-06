# Reporting Guide — AFW-DLV-0011.6

## Report Types
- SUSPICIOUS_ACTIVITY_REPORT
- CURRENCY_TRANSACTION_REPORT
- AML_MONITORING_REPORT
- FRAUD_INVESTIGATION_REPORT
- COMPLIANCE_CASE_REPORT
- REGULATORY_SUMMARY

## Workflow
DRAFT -> GENERATED -> UNDER_REVIEW -> APPROVED -> SUBMITTED -> ACCEPTED/REJECTED -> ARCHIVED or REGENERATED.

## Aggregation Strategy
Reports keep references and compact summaries from risk engines and compliance cases. Source datasets are not duplicated beyond required snapshot fields.

## Export Contents
JSON, CSV, and PDF exports include report reference, version, jurisdiction, period, summary, source references, decisions, checksum, and generation timestamp.
