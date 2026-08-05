# PRD — Card Tokenization Engine

## Summary
Provide an opaque tokenization layer for virtual and future physical cards that protects sensitive payment data while preserving lifecycle controls.

## Goals
- Generate opaque tokens for card references.
- Support token activation, suspension, resumption, rotation and revocation.
- Emit audit and telemetry information.

## Non-goals
- Full PAN or CVV storage.
- Direct issuer network integration.
