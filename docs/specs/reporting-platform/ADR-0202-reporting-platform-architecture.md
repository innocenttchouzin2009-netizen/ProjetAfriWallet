# ADR-0202 — Reporting Platform Architecture

## Status
Accepted

## Context
AfriWallet needs a BI surface that aggregates operational signals without coupling the user interface to transactional storage or duplicating domain workflows.

## Decision
Use a reporting platform built on read projections, aggregation services, and sandbox data adapters that can later be replaced by warehouse-backed projections.

## Consequences
- Positive: dashboards stay fast and isolated from transactional read pressure.
- Positive: analytical models can evolve independently.
- Trade-off: projection freshness and reconciliation must be governed explicitly.
