# AFW-DLV-0014.6 - Payment Provider Integration Platform

## Objective

Provide the production-ready integration boundary around payment providers while
preserving the provider-neutral execution contract introduced by AFW-DLV-0014.5.

## Responsibilities

- provider execution contracts
- environment-specific configuration model
- credential acquisition boundary
- external secret source boundary
- HMAC webhook signature verification foundation
- bounded exponential retries
- per-provider circuit breaker foundation
- provider health observations
- audit events and operational telemetry
- sandbox credential and executor adapters

## Architecture rule

AFW-DLV-0014.5 owns the provider-neutral Mobile Money gateway. AFW-DLV-0014.6
owns integration mechanics around provider adapters. It does not own Payment
Intent, Payment Routing, or Mobile Money business state.

Production connectors remain provider-specific adapters behind
`IProviderExecutor`, `IProviderCredentialService`, and
`IProviderWebhookVerifier`.

## Security boundary

AFW-DLV-0014.6 does not contain production provider credentials.

Real provider credentials:

- MUST come from an approved secret store
- MUST be environment-specific
- MUST never be committed
- MUST be rotated and audited outside source control

The included credential service and provider executor are sandbox adapters. They
are not certified integrations with Orange, MTN, Airtel, M-Pesa, or any other
operator production API.

## Delivery

AFW-DLV-0014.6