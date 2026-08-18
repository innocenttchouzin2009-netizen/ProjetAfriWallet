# ADR-0283

## Merchant Onboarding & Verification Orchestration

### Status

Accepted for AFW-DLV-0019.2.

### Decision

Introduce a separate merchant verification bounded context. It consumes merchant profile snapshots from AFW-DLV-0019.1. It does not mutate Merchant Registry business profile data.

### Document integrity

Documents preserve SHA-256 metadata. Duplicate hashes within one verification case are rejected.

### Verification provider

AFW-DLV-0019.2 includes a sandbox verification provider. No claim of external KYB-provider certification is made.

### Manual review

Verification may enter: `ManualReviewRequired`. This is a workflow state and not a regulatory verdict.

### Payment boundary

Merchant verification and merchant payment activation remain separate. A merchant may be `Verified` while:

- `paymentAcceptanceEnabled = false`
- `captureEnabled = false`
- `settlementEnabled = false`
- `payoutEnabled = false`
