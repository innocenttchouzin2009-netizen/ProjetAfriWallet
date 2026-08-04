# ADR-0074 — Explainable Fraud Rules

## Status
Accepted

## Context
The Sprint 5 payment engine needs an anti-fraud layer that is explicit, auditable, and versioned rather than opaque.

## Decision
The MVP fraud engine will rely on explainable rules and risk scoring that return a structured decision to the payment engine. Rule versions are preserved with every assessment.

## Consequences
- Fraud decisions are easier to audit.
- Operators can reason about why a payment was stepped up, reviewed, or blocked.
- The model remains simple enough for staging use.
