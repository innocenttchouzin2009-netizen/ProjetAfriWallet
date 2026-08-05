# ADR-0158 — Merchant Registry Architecture

## Status
Accepted

## Context
AfriWallet needs a first-class merchant domain that can support merchant registration, lifecycle management, and future onboarding, QR payment, POS, and settlement flows. The merchant stream should follow the same layered architecture used by the rest of the platform so that it can evolve without becoming a monolithic feature.

## Decision
We will introduce a dedicated Merchant module with separate domain, application, API, and scenario layers. Merchant registration, validation, status transitions, and lifecycle behavior will be implemented through a registry service that owns the core business rules. QR payment and settlement endpoints will be scaffolded as part of the same module, but they remain lightweight extensions for the current release.

## Consequences
- Merchant capabilities can evolve independently from wallet and card flows.
- The API surface is consistent with the rest of AfriWallet’s enterprise-style service design.
- Future onboarding and settlement work can extend the module without changing the registry contract.
