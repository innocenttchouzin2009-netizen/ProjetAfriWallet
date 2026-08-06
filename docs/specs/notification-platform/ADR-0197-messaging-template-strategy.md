# ADR-0197 — Messaging & Template Strategy

## Status
Accepted

## Context
Notifications span payment confirmations, fraud alerts, compliance workflows, developer events, and marketing communications. Template reuse and localization are required from the foundation.

## Decision
Use versioned and localized templates keyed by business event, with parameterized token replacement and locale fallback to English. Kiswahili is included from the initial set of supported locales.

## Consequences
- Positive: consistent messaging across channels.
- Positive: language expansion can be incremental.
- Trade-off: template governance and publication lifecycle become first-class operational concerns.
