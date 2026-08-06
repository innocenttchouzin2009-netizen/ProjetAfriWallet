# Operations Guide

## Deployment
1. Build and publish the RiskScoring.Api project.
2. Deploy the service behind the standard AfriWallet API gateway.
3. Validate the health and readiness endpoints before enabling traffic.

## Monitoring
- Watch for evaluation decisions trending toward manual review or block.
- Correlate audit events with transaction identifiers.
- Review telemetry duration and triggered-rule counts for regressions.
