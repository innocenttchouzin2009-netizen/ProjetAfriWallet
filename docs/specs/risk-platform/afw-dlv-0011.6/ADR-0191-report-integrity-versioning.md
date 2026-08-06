# ADR-0191 — Report Integrity & Versioning

## Status
Accepted

## Decision
Every workflow mutation after generation creates a new version with:
- author
- timestamp
- reason
- checksum
- status
- diff summary

## Integrity Model
- SHA-256 checksum generated from canonical report fields.
- Signature abstraction through IRegulatoryReportSigner.
- No private key material stored in repository.

## Consequences
- Full traceability over lifecycle transitions.
- Stable auditability for export and submission evidence.
- Future cryptographic signatures can be plugged in without changing domain schema.
