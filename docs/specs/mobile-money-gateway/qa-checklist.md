# AFW-DLV-0014.5 QA Checklist

## Provider registry

- [x] Registered providers can be resolved case-insensitively.
- [x] Duplicate provider codes are rejected during composition.
- [x] Unknown providers produce a domain error.
- [x] Country and currency capabilities are enforced before execution.

## Payment lifecycle

- [x] A valid request creates a payment and provider reference.
- [x] Duplicate idempotency keys return the existing payment.
- [x] Status polling updates the payment state.
- [x] Callback processing maps external provider status.
- [x] Invalid amounts are rejected.

## Operations

- [x] Payment lifecycle actions emit audit events.
- [x] Provider operations emit telemetry events.
- [x] Audit and telemetry collections support concurrent gateway access.

## Security

- [x] Only sandbox provider adapters are registered.
- [x] No operator credentials or secrets are stored in source files.
- [x] Production signature and authentication responsibilities are documented.

## Validation

- [x] Release API build succeeds with zero warnings.
- [x] Mobile Money scenarios pass.
- [x] Staged diff and credential-pattern checks are required before commit.