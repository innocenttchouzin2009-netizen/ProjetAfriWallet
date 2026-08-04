# ADR-0056: Atomic Transfer Execution

## Status
Accepted

## Context
Transfers must behave as atomic financial transactions. Any failure before the transfer completes should leave the system without partial ledger state, partial reservation consumption, or a partially completed payment intent.

## Decision
The transfer engine will execute as an atomic workflow: verify authorization and reservation, post the ledger transaction, consume the reservation, mark the payment intent completed, and publish downstream events as a single logical success path. Any failure before completion must result in rollback semantics for the transfer workflow.

## Consequences
- Transfers are safe against partial execution.
- The logic is easier to reason about and validate through scenarios.
- Future event publication can be added without changing the core atomicity model.
