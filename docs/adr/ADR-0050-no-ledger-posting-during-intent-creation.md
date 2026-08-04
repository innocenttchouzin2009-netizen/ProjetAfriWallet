# ADR-0050: No Ledger Posting During Intent Creation

## Status
Accepted

## Context
The payment intent phase must remain non-financial and side-effect free.

## Decision
Payment Intent creation must never write to the Universal Ledger or mutate balances. It only records the intent, its metadata, and its lifecycle state.

## Consequences
Accounting behavior remains isolated to later validation and execution stages.
