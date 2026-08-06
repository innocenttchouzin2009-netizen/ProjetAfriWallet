# ADR AFW-DLV-0011.4 — Device Intelligence Strategy

## Status
Accepted

## Context
The platform requires a device-intelligence capability to evaluate behavioral and device trust signals in a transparent, explainable way.

## Decision
Use a rule-based engine that scores device trust, reputation, network anonymity, environment changes, behavioral anomalies, and history to produce a human-readable decision and telemetry.

## Consequences
- Low implementation complexity.
- Explainable scoring and audit trail.
- Easy to extend with more advanced ML-based detection later.
