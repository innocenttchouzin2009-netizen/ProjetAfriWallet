# ADR-0159 — Merchant Identity Capabilities

## Status
Accepted

## Context
Merchant onboarding requires more than a simple registration record. The module must capture identity-related metadata, settlement preferences, and capability flags that can be reused by future KYC, QR payments, and settlement services.

## Decision
We will model merchant identity as a rich aggregate that includes business identity fields, operational capabilities, wallet association, and settlement preferences. Capability flags will be treated as first-class data so downstream services can determine supported behaviors without duplicated business logic.

## Consequences
- Merchant identity becomes extensible for future onboarding and compliance workflows.
- The registry can expose a consistent view of merchant readiness to other modules.
- The current release establishes a foundation for KYC, QR, and settlement integration without overcommitting to a full production implementation.
