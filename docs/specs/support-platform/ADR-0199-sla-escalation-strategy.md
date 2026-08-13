# ADR-0199 — SLA & Escalation Strategy

## Status
Accepted

## Context
Support cases require differentiated SLA objectives by priority, internal warnings before breach, and automatic escalation hooks for incidents at risk.

## Decision
Implement a configurable SLA policy engine with:
- first-response and resolution targets by priority;
- pause when waiting for customer;
- resume on reactivation;
- SLA warning before threshold;
- breach tracking with violation history;
- internal escalation notification for warning and breach.

## Consequences
- Positive: operations get early warning before SLA breaches.
- Positive: audit trail captures SLA decisions and breaches.
- Trade-off: status transitions must remain strict to avoid inaccurate SLA time accounting.
