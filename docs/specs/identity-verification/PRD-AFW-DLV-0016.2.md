# AFW-DLV-0016.2 — Identity Verification Orchestration Platform

## Objective
Provide a provider-neutral orchestration layer for identity verification.

## Supported verification types
- document
- selfie
- liveness
- composite verification

## Capabilities
- verification session lifecycle
- provider registry
- provider capability matching
- sandbox provider submission
- idempotent session creation
- provider reference correlation
- normalized verification result
- audit trail
- session expiry
- sandbox enforcement

## Integration boundary
The Compliance Profile created by AFW-DLV-0016.1 remains the authoritative internal KYC registry. AFW-DLV-0016.2 orchestrates verification but does not replace that registry.

## Sensitive data
Raw identity documents, selfies and biometric templates are not stored inside the orchestration domain. Only opaque references and normalized provider results may cross the provider boundary.

## Production boundary
All providers implemented by this delivery are sandbox providers. No production KYC provider is enabled.

## Out of scope
- sanctions screening
- PEP screening
- AML monitoring
- production biometric verification
- regulatory certification
- production provider credentials

## Decision
Successful validation means: READY FOR REVIEW

It does not mean:
- IDENTITY CERTIFIED
- KYC CERTIFIED
- PRODUCTION PROVIDER APPROVED
