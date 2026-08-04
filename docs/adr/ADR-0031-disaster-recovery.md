# ADR-0031: Disaster Recovery for Ledger and Projection Recovery

- Status: Accepted
- Date: 2026-08-04

## Context
AfriWallet requires production-ready recovery controls for financial data, projections, and outbox replay after incidents. The platform needs a repeatable mechanism to create backups, restore them, validate ledger integrity, and perform point-in-time recovery.

## Decision
We will implement a dedicated disaster-recovery module with:
- backup creation and retention metadata,
- restore execution into a target environment,
- point-in-time recovery requests,
- ledger integrity validation,
- outbox replay and projection rebuild guidance,
- operational runbooks and scenario-based validation.

## Consequences
This provides a baseline for incident response and is intentionally lightweight to remain runnable and testable in the current repository while still aligning with production objectives.
