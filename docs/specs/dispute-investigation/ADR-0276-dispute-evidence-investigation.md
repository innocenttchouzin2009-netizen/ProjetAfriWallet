# ADR-0276 - Evidence & Investigation Platform

## Status

Accepted for AFW-DLV-0018.3.

## Decision

Introduce an independent investigation bounded context that manages evidence collection and case lifecycle for a dispute claim already gated by the eligibility decision. It consumes eligibility snapshots rather than modifying the Dispute Registry or the Eligibility engine.

## Evidence integrity

Every evidence item carries a SHA-256 hash, size, and content type. Duplicate hashes within the same investigation are rejected.

## Determinism

Evidence requests are automatically fulfilled by the first matching open request of the same evidence type. The investigation advances to `UnderReview` only when no open evidence requests remain.

## Execution boundary

Investigation outcome is an analyst conclusion, not a refund decision, chargeback execution, or money movement.
