# ADR-0090 — Invoice Idempotency and Retry Strategy

## Status
Accepted

## Context
Repeated invoice creation requests and transient payment failures must not create duplicates or leave the system in an inconsistent state.

## Decision
We will enforce invoice idempotency by reusing an existing invoice for the same subscription and billing period, and we will apply a bounded retry strategy for payment attempts.

## Consequences
- Duplicate billing records are avoided
- Payment retries remain controlled and observable
- The system is better prepared for transient gateway failures
