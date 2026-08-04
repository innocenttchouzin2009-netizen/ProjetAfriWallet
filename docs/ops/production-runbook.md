# Production runbook

## Health endpoints
- `GET /health` returns service-level health.
- `GET /health/live` checks the process is alive.
- `GET /health/ready` checks readiness for traffic.

## Observability
- Request correlation headers: `x-correlation-id`, `x-request-id`, `x-session-id`, `x-awid`.
- Structured logs include trace, correlation, request, session, wallet, and duration metadata.
- Metrics and tracing should be exported to Prometheus and Grafana via the Docker Compose stack.

## Deployment checklist
1. Build and run the services with Docker Compose.
2. Verify `/health`, `/health/live`, and `/health/ready`.
3. Validate the CI workflow runs successfully.
4. Confirm logs and metrics are visible in the observability stack.
