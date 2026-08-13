# ADR-0239 - Payment Operational Validation Strategy

## Status

Accepted for AFW-DLV-0014.7.

## Decision

Operational readiness is validated through four evidence classes:

1. Functional evidence from the six real scenario executables.
2. Build and supply-chain evidence from Release builds, secret scanning, and
   vulnerable-package scanning.
3. Static implementation evidence for health, correlation, audit, telemetry,
   metrics, retry, circuit breaking, provider health, webhooks, idempotency, and
   recovery.
4. Packaging evidence from required release inputs, a JSON manifest, SHA-256
   checksums, and independent package verification.

## CI strategy

Pull-request CI runs `validate-payment-platform.ps1` on Windows with .NET 10. A
successful checkout without the readiness command is not accepted as production
readiness evidence.

## Failure policy

The validator does not skip unavailable checks. Any absent prerequisite, failed
scenario, vulnerable dependency, likely secret, missing package input, or checksum
mismatch produces `NOT READY` and a nonzero exit code.