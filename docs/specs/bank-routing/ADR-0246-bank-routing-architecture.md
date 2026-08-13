# ADR-0246 — Bank Routing Architecture

## Status

Accepted

## Context

AFW-DLV-0015.2 creates a transfer intent and marks it as ReadyForRouting. The next logical step is to choose the best eligible bank rail without performing any bank-side execution.

## Decision

Introduce a dedicated BankRouting aggregate and service that evaluates the rail registry, scores eligible candidates, and records a routing decision. The decision is explicit, auditable, and deterministic.

## Consequences

- routing logic is isolated from execution logic
- rail selection remains explainable and testable
- future execution layers can rely on a single, stable routing decision
