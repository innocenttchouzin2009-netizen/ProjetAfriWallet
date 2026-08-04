# ADR-0031: Provider Connectors for Subscription Operations

- Status: Accepted
- Date: 2026-08-04

## Context

AfriWallet needs a pluggable integration layer for external subscription providers such as Netflix, Canal+, MyBouquetAfricain, and Cinaf. The connector layer must normalize operation semantics, expose provider capabilities, support health checks, and protect activation flows from duplicate or conflicting requests.

## Decision

We introduce a connector abstraction with a central registry that exposes:

- capability discovery for activation, renewal, suspension, resumption, cancellation, and status lookup;
- operation methods for activate, renew, suspend, resume, cancel, and status;
- health checks for provider availability;
- sandbox implementations for the initial provider set;
- internal HTTP endpoints to let the subscriptions platform invoke connector operations consistently.

The registry also tracks request IDs per provider/subscription pair to handle duplicate replay safely and surface conflicts when a second activation request arrives for the same active subscription.

## Consequences

This approach keeps provider-specific logic behind a stable interface and makes it straightforward to add additional connectors later. It also improves operational safety by preventing ambiguous or duplicate activation requests from propagating into downstream integrations.
