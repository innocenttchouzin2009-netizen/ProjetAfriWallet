# AFW-DLV-0009.4 — Merchant API & POS

## Summary
The merchant POS module enables terminals and checkout flows to create transactions, maintain heartbeats, and produce receipts without moving funds directly. Payments are represented as transfer intents for orchestration by the payment gateway.

## Scope
- Register POS terminals
- Maintain heartbeat activity
- Create checkout and payment transactions
- Complete transactions and generate receipts
- Emit audit and telemetry events
