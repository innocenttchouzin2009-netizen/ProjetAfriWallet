# AFW-DLV-0009.2 — Merchant Onboarding & KYC PRD

## Summary
Deliver a merchant onboarding engine that supports profile collection, required-field validation, KYC case creation, approval and rejection flows, and merchant activation.

## Functional requirements
- Start onboarding for a merchant.
- Complete the merchant profile.
- Validate required onboarding fields.
- Create and manage a KYC case.
- Approve or reject the KYC case.
- Activate the merchant after successful onboarding.

## Non-functional requirements
- Keep the onboarding domain provider-agnostic.
- Provide audit and telemetry events for operational visibility.
- Keep the API behavior predictable and scenario-testable.
