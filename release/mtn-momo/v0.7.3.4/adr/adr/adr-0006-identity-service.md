# ADR-0006 — Identity Service

## Status
Accepted

## Context
AfriWallet needs a shared, reusable identity foundation for authentication, OTP, PINs, devices, sessions and AWID.

## Decision
We will introduce a dedicated Identity Service that other services consume instead of re-implementing authentication flows.

## Consequences
- Shared identity lifecycle across products
- Simpler compliance and security management
- Clear API contract for mobile and backend
