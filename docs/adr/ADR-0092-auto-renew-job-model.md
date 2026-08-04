# ADR-0092 — Auto-Renew Job Model

## Status
Accepted

## Context
Subscription renewals need a formal execution unit that can be scheduled, retried, and audited independently of the billing and lifecycle engines.

## Decision
We will introduce an AutoRenewJob entity that represents a renewal attempt, carries retries and status, and can be processed by an internal job runner.

## Consequences
- Renewal work becomes observable and debuggable
- Scheduling and execution logic can evolve independently
- The system is prepared for orchestration with real schedulers later on
