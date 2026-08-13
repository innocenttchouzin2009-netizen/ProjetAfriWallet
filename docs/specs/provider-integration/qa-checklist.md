# AFW-DLV-0014.6 QA Checklist

## Scope

- [x] Only ProviderIntegration source, scenarios, and documentation are included.
- [x] No generated `bin` or `obj` paths are tracked.
- [x] Sandbox and production responsibilities are documented separately.

## Execution and resilience

- [x] Successful execution returns a provider reference.
- [x] Non-retryable failures are not retried.
- [x] Retryable results honor the configured retry count.
- [x] Transient executor exceptions are normalized and retried.
- [x] Cancellation is propagated.
- [x] The per-provider circuit opens after five failed requests.

## Security

- [x] No production provider credential is present in source control.
- [x] Webhook secrets are resolved at runtime.
- [x] Valid HMAC-SHA256 signatures are accepted.
- [x] Malformed and mismatched signatures are rejected.
- [x] Binary digests use a timing-safe comparison.

## Operations

- [x] Provider health records success rate and latency.
- [x] Execution emits audit events.
- [x] Execution emits telemetry events.
- [x] API health identifies sandbox composition.

## Validation

- [x] Release API build passes with zero warnings.
- [x] Provider integration scenarios pass.
- [ ] Pull request CI is green.
- [ ] Squash SHA and remote tag parity are verified.