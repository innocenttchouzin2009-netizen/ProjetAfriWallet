# ADR-0082: Provider Independence

## Status
Accepted

## Context
External subscription providers evolve independently and may change their APIs, credentials, or partner flows. A direct dependency on each provider would make the platform fragile.

## Decision
Each provider will be isolated behind a connector abstraction. The registry owns provider metadata, while connector implementations handle provider-specific operations and can be evolved independently.

## Consequences
- Provider changes are localized and easier to test.
- The platform can support direct API, voucher, redirect, and partner flows without coupling them to the registry layer.
