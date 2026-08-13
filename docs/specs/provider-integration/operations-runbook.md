# Provider Integration Operations Runbook

## Pre-deployment checks

1. Confirm the deployment is explicitly sandbox or production.
2. Confirm production adapters replace all sandbox credential and executor
   registrations.
3. Confirm secret lookup keys exist in the approved secret store without printing
   their values.
4. Confirm provider base URIs, timeouts, retry limits, and enabled states.
5. Confirm webhook timestamp, replay, and idempotency controls for each connector.
6. Run the Release build, scenarios, dependency scan, and secret scan.

## Runtime endpoints

- `GET /health` reports service health and sandbox composition mode.
- `POST /api/v1/provider-integration/execute` executes through resilience policy.
- `GET /api/v1/provider-integration/providers/{providerCode}/health` reports
  process-local provider observations.
- `POST /api/v1/provider-integration/webhooks/{providerCode}/verify` verifies the
  configured HMAC foundation.
- operations audit and telemetry endpoints expose in-memory delivery events.

## Circuit-open response

A retryable HTTP 503 response with `circuit_open` means the provider circuit has
reached its failure threshold. Do not bypass the circuit. Check provider status,
credentials, network reachability, error mappings, and rate limits.

## Webhook verification incidents

1. Preserve event identifiers and timestamps without recording secret values.
2. Confirm the provider code and configured key identifier.
3. Confirm raw payload handling has not changed.
4. Check clock skew and replay controls in the provider adapter.
5. Rotate compromised material using the approved secret process.

## Rollback

Disable the affected provider through environment-specific configuration or roll
back the connector deployment. Never commit emergency credentials or bypass
signature verification.