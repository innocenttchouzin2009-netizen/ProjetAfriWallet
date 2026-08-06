# ADR-0186 — Risk Scoring Implementation

## Status
Accepted

## Context
The risk platform required a unified scoring layer that could combine multiple signals into a single explainable decision. The implementation needed to remain simple enough for initial release while still producing auditability and telemetry.

## Decision
Use a weighted aggregation model with explicit factor weights and a score-to-decision mapping that yields allow, challenge, manual review, and block outcomes.

## Consequences
- The engine produces deterministic and explainable factors.
- The scoring thresholds are easy to tune without changing core logic.
- Scenario-based validation provides regression protection for the decision behavior.
