# ADR-0089 — Billing Cycle Model

## Status
Accepted

## Context
Billing must support one-time, monthly, quarterly, semi-annual, and annual invoices in a consistent way.

## Decision
We will model billing cycles explicitly in the subscriptions domain and use them to drive invoice creation and due-date preparation.

## Consequences
- Billing rules are explicit and testable
- Future invoicing strategies can be introduced without changing the contract
- The domain remains compatible with both manual and automated billing workflows
