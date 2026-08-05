# ADR-0160 — Merchant Onboarding Architecture

## Status
Accepted

## Context
AfriWallet needs a professional onboarding engine that can collect merchant profile data, prepare a KYC case, and support future integrations with external providers without coupling the domain to vendor-specific implementations.

## Decision
We will implement a merchant onboarding module with a dedicated onboarding service, profile model, validator, and KYC case workflow. The domain remains provider-agnostic and can later be adapted to real KYC adapters through the existing service layer.

## Consequences
- Merchant onboarding can evolve independently from specific KYC providers.
- The business rules remain stable while adapters change over time.
- The domain is ready for future compliance and AML integrations.
