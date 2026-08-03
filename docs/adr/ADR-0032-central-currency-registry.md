# ADR-0032 — Central Currency Registry

## Status
Accepted

## Context
AfriWallet needs a single source of truth for currency metadata. Wallets, the ledger, payments, and the FX engine should consume the same contract rather than maintaining independent lists of currencies.

## Decision
The Currency Registry becomes the canonical owner of currency metadata, including code, numeric code, name, minor units, symbol, and lifecycle status. All downstream components must rely on the registry interface rather than maintaining their own currency list.

## Consequences
- A single registry owns currency metadata.
- Wallet creation checks are centralized.
- FX and ledger flows use the same active/disabled/retired rules.
- Currency updates become easier to audit and reason about.
