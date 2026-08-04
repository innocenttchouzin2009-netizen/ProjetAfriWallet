# ADR-0092: Caching Strategy

## Status
Accepted

## Context
The platform needs a lightweight cache layer for read-heavy configuration data while avoiding caching mutable financial state.

## Decision
A memory cache is used for read-optimized data such as currencies, FX, subscriptions, configuration, and fraud rules. Balances, ledger entries, and transactions are intentionally excluded from caching.

## Consequences
The platform gains improved read performance without compromising consistency of financial data.
