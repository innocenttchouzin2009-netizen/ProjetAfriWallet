# ADR-0275 - Dispute Eligibility & Classification Engine

## Status

Accepted for AFW-DLV-0018.2.

## Decision

Introduce an independent eligibility and classification bounded context. It consumes claim and transaction snapshots rather than modifying the Dispute Registry or the Payment Platform.

## Explainability

Every decision preserves rule-level evaluations with an explicit rule code, outcome, and reason.

## Determinism

The same claim and transaction snapshot always produce the same classification and eligibility result.

## Execution boundary

Eligibility is not refund authorization, and classification is not chargeback execution. No money movement occurs in this delivery.
