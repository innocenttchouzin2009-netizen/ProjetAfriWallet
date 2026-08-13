# AFW-DLV-0015.2 — Bank Transfer Intent Engine

## Objective

Define and manage the lifecycle of an AfriWallet external bank
transfer before routing or execution.

## Responsibilities

- transfer intent creation
- beneficiary and bank-account references
- amount and currency validation
- idempotency
- confirmation lifecycle
- ready-for-routing state
- cancellation
- expiration foundation
- owner-level listing
- audit foundation
- telemetry foundation

## Architecture boundary

This engine does not select a banking rail.

It does not execute a bank transfer.

It does not communicate directly with a bank.

Routing belongs to AFW-DLV-0015.3.

Execution belongs to AFW-DLV-0015.4.
