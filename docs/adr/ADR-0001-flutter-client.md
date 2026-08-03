# ADR-0001 — Flutter for the mobile client

- Status: Accepted
- Date: 2026-08-02

## Decision

Use Flutter for the AfriWallet Android and iOS client.

## Consequences

A shared Dart codebase can implement the AfriWallet experience while still
allowing platform-specific integrations for biometrics, NFC, secure storage,
Apple Pay, and Google Wallet where contracts and platform capabilities permit.
