# ADR-0196 — Notification Platform Architecture

## Status
Accepted

## Context
AfriWallet needs a centralized notification platform that supports multiple channels, user preferences, localized templates, traceability, and extensibility without coupling each business domain to a delivery provider.

## Decision
Adopt a modular platform with Domain, Application, Infrastructure, Contracts, Api, and scenario validation layers. Delivery is orchestrated through a central notification service, channel dispatching, preference evaluation, retry handling, and template rendering.

## Consequences
- Positive: provider and channel expansion can happen without changing API contracts.
- Positive: notification audit and telemetry remain centralized.
- Trade-off: orchestration layer must stay disciplined to avoid becoming a provider-specific blob.
