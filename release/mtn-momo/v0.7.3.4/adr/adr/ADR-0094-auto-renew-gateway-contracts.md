# ADR-0094 — Auto-Renew Gateway Contracts

## Status
Accepted

## Context
The auto-renew engine needs to interact with billing, lifecycle, and notification concerns without hard-coding their implementations.

## Decision
We will define gateway contracts for billing, lifecycle, and notification operations and keep the current implementations as in-memory or fake adapters for development and testing.

## Consequences
- The engine remains loosely coupled from downstream systems
- Production adaption is straightforward when real engines are available
- Tests remain deterministic and isolated
