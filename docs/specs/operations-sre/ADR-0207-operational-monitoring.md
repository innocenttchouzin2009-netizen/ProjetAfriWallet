# ADR-0207 - Operational Monitoring

## Decision
Model service state through a Health Aggregator Service that collects per-service status, computes uptime, and exposes dashboard-ready summaries.

## Rationale
- Prevents duplicate logic in endpoint handlers.
- Gives a single source of truth for uptime and service counts.
- Makes Prometheus and OpenTelemetry integration straightforward.

## Consequences
- Dashboard metrics are derived from the aggregator.
- Future collectors can be added without changing the API contract.