# ADR-0274 - Dispute Case & Claim Registry

## Status

Accepted for AFW-DLV-0018.1.

## Decision

Introduce a Dispute bounded context that owns claim identity and lifecycle. The registry stores opaque references to the Payment, Banking, Compliance, and Fraud platforms and never calls their implementations.

## Immutability

Claim history is append-only. Closed, rejected, and cancelled claims are immutable.

## Execution boundary

The registry records claims and outcomes only. Refund decisioning, chargeback execution, recovery, and ledger mutation require separately governed deliveries.
