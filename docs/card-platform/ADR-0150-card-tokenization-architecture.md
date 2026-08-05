# ADR-0150 — Card Tokenization Architecture

## Status
Accepted

## Context
AfriWallet needs a reusable card tokenization layer that can protect sensitive card data across virtual cards, wallet integrations, NFC and merchant flows.

## Decision
We will implement a dedicated card tokenization service in CardPlatform that generates opaque tokens, stores only references in the application layer and exposes lifecycle operations through the API.

## Consequences
- Sensitive card data is not exposed through application workflows.
- The same token model can be reused by virtual cards, physical cards and wallet integrations.
- Token lifecycle events become auditable and observable.
