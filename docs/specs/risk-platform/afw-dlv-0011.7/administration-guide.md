# Administration Guide — AFW-DLV-0011.7

## Configuration
- Keep `RiskProduction:EnableProductionMode` disabled by default.
- Inject runtime values through environment variables, especially `AFW_RISK_INTERNAL_API_KEY`.
- Keep retry and circuit-breaker thresholds aligned with production SLOs.

## Operational Endpoints
- `/health/live` for process liveness.
- `/health/ready` for dependency readiness.
- `/health/startup` for startup config validation.
- `/metrics` available only when feature-flagged and internally authorized.

## Security
- Never commit real secrets in `appsettings*`.
- Internal endpoints require `X-Internal-Key`.
- Preserve correlation IDs across API boundaries.
