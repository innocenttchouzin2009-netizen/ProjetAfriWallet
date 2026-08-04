# ADR-0031 — Subscription Catalog Engine

## Status
Accepted

## Context
AfriWallet requires a subscription catalog capability to expose curated offers from providers, including country and currency-aware filtering, featured offers, search, sorting, and pagination.

## Decision
We will introduce a dedicated subscription catalog engine in the subscriptions domain with:
- provider and plan metadata exposure
- catalog offer retrieval by country, currency, category, feature flag, and textual query
- sorting and pagination support for offer listings
- an API contract under /api/v1/subscriptions/catalog and /api/v1/subscriptions/providers

## Consequences
- Subscription offers can be surfaced consistently to mobile and partner clients
- Catalog behavior becomes testable and extensible for future pricing and promotion rules
- The domain is prepared for later integration with live provider feeds and billing backends
