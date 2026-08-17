# ADR-0270 - Fraud Investigation & Response Platform

## Status

Accepted for AFW-DLV-0017.5.

## Decision

Introduce a Fraud Investigation bounded context that retains fraud-decision references as evidence and manages the human-review lifecycle:

`Open -> Assigned -> UnderInvestigation -> Escalated (optional) -> Resolved -> Closed`

Historical evidence is retained and never silently overwritten. Recommendations are stored but never executed by this delivery.