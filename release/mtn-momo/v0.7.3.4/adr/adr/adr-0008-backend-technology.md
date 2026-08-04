# ADR-0008 — Backend Technology

## Status
Accepted

## Context
AfriWallet requires a robust backend for identity, ledger, payments and fraud prevention with strong typing and a mature ecosystem.

## Decision
We will use ASP.NET Core with .NET 10 LTS, PostgreSQL and Entity Framework Core for the initial backend foundation, with a modular monolith architecture that can evolve into microservices.

## Consequences
- Strong typing and mature tooling for critical financial flows
- Clear separation between services and modules
- Good support for APIs, async workers and integration testing
