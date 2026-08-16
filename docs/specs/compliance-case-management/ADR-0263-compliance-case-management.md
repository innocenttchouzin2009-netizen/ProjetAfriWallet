# ADR-0263 - Compliance Case Management

## Decision

Investigations are represented by a lifecycle aggregate. Source engines cross the boundary only as immutable references and summaries. Notes and audit events are append-only; closed cases are immutable.

## Non-goals

No screening, AML rules, scoring, regulatory filing, or legal determination is implemented here.