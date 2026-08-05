# ADR-0148 — Card Authorization Engine

## Status
Accepted

## Context
AfriWallet requires a real-time card authorization decision capability that can evaluate card eligibility, controls, limits, balance and fraud signals before approving or declining a transaction.

## Decision
We will implement a card authorization engine inside the CardPlatform service layer that evaluates transactions in a deterministic order: card status, card controls, limits, wallet balance, fraud and risk signals, then emits a decision plus reason code.

## Consequences
- Enables sandbox-safe real-time decisions for virtual cards.
- Provides a consistent decision model for future network integration.
- Keeps fraud and risk evaluation decoupled from the core decision engine.
