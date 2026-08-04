# ADR-0081: Subscriptions Reuse the Payment Engine

## Status
Accepted

## Context
Recurring payments should not require a separate execution engine when the existing wallet and payment flow already supports intent, authorization, transfer, settlement, and receipts.

## Decision
AfriSubscriptions will reuse the existing payment engine primitives for validation, authorization, execution, transfer, and settlement. Provider-specific operations remain isolated behind connectors and repositories.

## Consequences
- The subscription layer benefits from the same security and observability controls as the rest of the wallet platform.
- Future recurring-payment scenarios can be implemented without duplicating payment infrastructure.
