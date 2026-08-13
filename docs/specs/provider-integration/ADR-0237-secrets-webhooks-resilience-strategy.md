# ADR-0237 - Secrets, Webhooks, and Resilience Strategy

## Status

Accepted for AFW-DLV-0014.6.

## Context

Provider integrations require credentials and webhook authenticity while facing
transient upstream failures. Source-controlled secrets, naive signature checks,
and unbounded retries are unacceptable.

## Decision

1. Resolve secret values at runtime through `IProviderSecretSource`.
2. Keep production credential acquisition behind
   `IProviderCredentialService`.
3. Verify HMAC-SHA256 signatures over the exact payload bytes and compare binary
   digests with `CryptographicOperations.FixedTimeEquals`.
4. Retry only retryable results or normalized transient executor exceptions,
   using a bounded exponential delay.
5. Open a per-provider circuit after five failed integration calls for 30
   seconds.
6. Record one health observation, audit event, and telemetry event for each
   completed integration request.

## Consequences

- No provider secret value is required in repository content.
- Invalid hexadecimal signatures are rejected without exceptions escaping the
  verifier.
- Retry count is bounded and cancellation remains cooperative.
- Circuit and health state reset when the process restarts.
- Production webhook adapters must additionally enforce timestamps, replay
  prevention, event idempotency, and each provider's canonicalization rules.