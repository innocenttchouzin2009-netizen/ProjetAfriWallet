# ADR-0266 - Fraud Signal & Event Platform

## Decision

Fraud receives provider-neutral canonical signals instead of direct dependencies on every source domain. Each signal has immutable event ID, source, type, severity, generic subject, occurrence/recording timestamps, and metadata.

`EventId` is the idempotency boundary. Compliance remains independent; Fraud may consume compliance evidence without reimplementing KYC, screening, AML, or case management.

AFW-DLV-0017.1 records evidence only. Scoring and decisioning belong to later Sprint 17 deliveries.