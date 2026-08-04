# ADR-0080: Subscription Providers Are Configurable

## Status
Accepted

## Context
AfriWallet needs a recurring-payment surface that can grow beyond a hard-coded list of providers. The provider catalog should be configurable and evolvable without redeploying the backend.

## Decision
Subscription providers and their plans will be registered in a configurable registry backed by a repository and exposed through a versioned API. The initial implementation uses an in-memory repository, which can later be swapped for a persistent store.

## Consequences
- Providers can be added, suspended, or updated without changing the application code.
- The API can expose search, filtering, and plan listing in a consistent way.
- The design supports future connector-based integrations for external providers.
