# ADR-0048: Intent Before Execution

## Status
Accepted

## Context
Payments must begin with a persistent, traceable intention before any financial movement is executed.

## Decision
Every payment operation will start with a Payment Intent that is created, validated, and stored before any ledger posting or balance mutation occurs.

## Consequences
The payment stack can evolve independently from accounting execution and remains idempotent and auditable.
