# ADR-0091 — Payment Intent Adapter

## Status
Accepted

## Context
The billing engine must remain decoupled from payment provider specifics while still supporting real payment processing in later phases.

## Decision
We will introduce an abstraction for payment intent submission, with a development-only FakePaymentIntentGateway for tests. In the final integration, this gateway will be replaced by an adapter to the real Payment Intent Engine.

## Consequences
- The billing engine can evolve independently of gateway changes
- Testability remains high without affecting production architecture
- The path to real payment integration is explicit and controlled
