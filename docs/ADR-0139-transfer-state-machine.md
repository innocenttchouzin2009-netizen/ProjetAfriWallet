# ADR-0139 — Transfer State Machine

## Status
Accepted

## Context
Transfer intents require a predictable sequence of states that can be audited and reasoned about across providers.

## Decision
The engine uses the following lifecycle: CREATED → VALIDATED → FUNDS_RESERVED → READY → SUBMITTED → PROCESSING → COMPLETED. Failures move to FAILED, cancellations before submission move to CANCELLED, and expired intents move to EXPIRED.

## Consequences
- State transitions become explicit and testable.
- Downstream processors can react to stable states.
- Operators gain a clear view of pending versus completed transfers.
