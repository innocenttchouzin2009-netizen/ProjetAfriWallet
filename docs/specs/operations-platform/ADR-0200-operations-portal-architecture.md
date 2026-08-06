# ADR-0200 — Operations Portal Architecture

## Status
Accepted

## Context
AfriWallet needs a centralized internal portal to inspect, supervise, and act on operational data from Wallet, Payments, Banking, Cards, Merchant, Subscriptions, Risk, Support, and Notifications without duplicating the business logic.

## Decision
Adopt a portal composed of a thin UI shell over dedicated backend read and action services. Sensitive actions remain guarded by strong confirmation and auditable workflows.

## Consequences
- Positive: teams can work from a single internal surface.
- Positive: portal actions stay bounded and auditable.
- Trade-off: read-model aggregation must be carefully maintained.
