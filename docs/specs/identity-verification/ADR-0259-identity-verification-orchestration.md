# ADR-0259 — Identity Verification Orchestration

## Status
Accepted for AFW-DLV-0016.2.

## Decision
Identity verification is implemented behind provider-neutral interfaces. The Compliance domain must not depend directly on a specific KYC vendor.

## Provider boundary
Providers expose:
- capabilities
- health/status
- submission
- provider reference

Provider-specific payloads remain outside the core domain.

## Sandbox
AFW-DLV-0016.2 permits sandbox providers only. Production providers require a separate controlled delivery.

## Security
Provider callbacks must eventually be authenticated cryptographically. Raw identity documents and biometric material must not be stored in the core orchestration aggregate.
