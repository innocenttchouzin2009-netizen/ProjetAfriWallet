# ADR-0088: JWT Hardening

## Status
Accepted

## Context
JWTs must be validated strictly to avoid accepting malformed or expired tokens in production.

## Decision
The platform enforces strong JWT validation requirements: non-empty issuer, audience, subject, and key id; short-lived expiration; and explicit rejection logic for invalid or expired claims.

## Consequences
The service can reject malformed or expired tokens early and consistently.
