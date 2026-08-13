# AFW-DLV-0015.3 — Bank Routing & Rail Selection Engine

## Objective

Evaluate available bank rails for a transfer intent that is already ReadyForRouting and select the best deterministic candidate without issuing any bank order.

## Boundaries

- Receives a transfer intent context.
- Determines eligible rails.
- Applies country, currency, amount, health, and priority constraints.
- Produces an explicit routing decision with a reason and fallback order.
- Does not send a bank instruction.
- Does not execute a transfer.
- Does not connect to a live banking provider.

## Out of scope

- execution of cross-border or local transfer orders
- connector configuration for actual banks
- payment initiation APIs
- reconciliation and settlement
