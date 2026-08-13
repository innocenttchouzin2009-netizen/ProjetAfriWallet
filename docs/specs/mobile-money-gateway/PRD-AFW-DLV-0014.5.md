# AFW-DLV-0014.5 - Mobile Money Gateway

## Objective

Provide a provider-neutral Mobile Money integration boundary for the AfriWallet
Payment Orchestration Platform.

## Responsibilities

- provider discovery and registry
- payment initiation
- provider transaction references
- payment status polling
- callback foundation
- country and currency compatibility
- idempotency
- audit events
- operational telemetry
- sandbox provider adapters

## Architecture rule

The Mobile Money Gateway does not own Payment Intent or Payment Routing.

Payment Intent defines what must be paid.

Payment Routing determines which payment rail and provider should be used.

Mobile Money Gateway executes the selected Mobile Money route through a
provider-neutral interface.

## Security

Provider credentials MUST NOT be committed to source control.

Real provider credentials must be supplied through approved secret and
configuration infrastructure.

The ORANGE, MTN, AIRTEL, and MPESA registrations in this delivery are sandbox
adapters. They are not certified integrations with operator production APIs.

## Delivery

AFW-DLV-0014.5