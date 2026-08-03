# ADR-0007 — Backend Agnostic Clients

## Status
Accepted

## Context
AfriWallet must keep its business logic independent from any single client implementation.

## Decision
All backend services will expose versioned HTTP contracts and remain client-agnostic.

## Consequences
- Flutter, web, admin portals and partner integrations can reuse the same API.
- Business logic remains portable and easier to evolve.
