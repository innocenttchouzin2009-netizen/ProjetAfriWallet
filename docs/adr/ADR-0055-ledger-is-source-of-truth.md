# ADR-0055: Ledger Is The Source of Truth

## Status
Accepted

## Context
Wallet-to-wallet transfers must be represented as accounting transactions rather than direct balance mutations. The system already has a universal ledger and balance projection layer, so the transfer engine should use the ledger as the authoritative source of financial truth.

## Decision
Wallet-to-wallet transfers will create ledger entries exclusively through the Universal Ledger. Wallet balances will be reconstructed from ledger projections and will not be modified directly by transfer execution.

## Consequences
- Financial state is auditable and consistent.
- Balance projections remain derived rather than authoritative.
- Transfer execution becomes easier to reason about and test.
