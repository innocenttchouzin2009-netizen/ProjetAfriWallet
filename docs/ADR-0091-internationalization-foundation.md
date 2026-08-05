# ADR-0091 — Internationalization Foundation

## Status
Accepted

## Context
AfriWallet needs a scalable localization foundation that can support Kiswahili as an official language and future African languages such as Amharic, Hausa, Yoruba, and Zulu without a rewrite.

## Decision
We will introduce a localization architecture for Flutter based on ARB resources, a dedicated locale controller, and a repository-backed persistence layer. On the backend, we will expose profile language endpoints to support language, culture, and timezone preferences.

## Consequences
- The app can switch languages instantly without restarting.
- The language choice persists between sessions.
- The app can support future locales through the same mechanism.
- The backend can provide multilingual preferences for downstream services.
