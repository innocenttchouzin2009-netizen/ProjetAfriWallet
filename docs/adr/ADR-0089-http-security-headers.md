# ADR-0089: HTTP Security Headers

## Status
Accepted

## Context
Web APIs should be hardened against common browser-based attacks by emitting secure HTTP headers by default.

## Decision
The platform emits HSTS, CSP, X-Content-Type-Options, Referrer-Policy, X-Frame-Options, and Permissions-Policy headers for all HTTP responses.

## Consequences
Browsers receive a stronger security posture with less manual configuration.
