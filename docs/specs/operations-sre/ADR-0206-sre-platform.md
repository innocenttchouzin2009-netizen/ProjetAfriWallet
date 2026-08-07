# ADR-0206 - SRE Platform

## Decision
Use a dedicated Operations Center bounded context for production supervision instead of embedding operational state computation directly in the API layer.

## Rationale
- Keeps health aggregation testable and reusable.
- Enables future collectors for Wallet, Identity, Merchant, Risk, Notifications, and Reporting.
- Separates operational orchestration from transport concerns.

## Consequences
- The API becomes a thin transport surface.
- The application layer owns aggregation and policy enforcement.