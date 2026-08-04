# ADR-0124 — OpenTelemetry Standardization

## Status
Accepted

## Context
MobileMoney operations need consistent telemetry across traces, metrics, and diagnostics.

## Decision
The MobileMoney API will emit standardized activities and metrics for key transaction phases and expose internal diagnostics for the telemetry configuration.

## Consequences
- Distributed tracing becomes available for the MTN MoMo flow.
- Operational metrics can be surfaced in dashboards.
- Sensitive data remains excluded from telemetry tags.
