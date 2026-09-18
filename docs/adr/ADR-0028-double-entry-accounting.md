# ADR-0028 — Double Entry Accounting

Status: Accepted

## Context

AfriWallet requires a ledger model that guarantees integrity across multi-wallet and multi-currency financial flows.

## Decision

Every financial transaction must produce balanced ledger entries in the same currency such that the sum of debits equals the sum of credits.

## Consequences

- Transactions are rejected when debit and credit totals diverge.
- The ledger can serve as the single source of truth for balances.
- Reversals remain explicit and auditable without mutating posted entries.
