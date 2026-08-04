# ADR-0090: API Key Lifecycle

## Status
Accepted

## Context
API keys must be managed with clear lifecycle rules to prevent stale or revoked keys from being accepted by the platform.

## Decision
The platform models API keys with status, creation date, expiry date, and last-used timestamps. Revoked or expired keys are rejected.

## Consequences
The backend keeps a simple yet auditable lifecycle model for API keys that can be extended to encrypted storage later.
