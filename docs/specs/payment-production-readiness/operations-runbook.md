# Payment Platform Operations Runbook

## Before deployment

1. Verify all 22 readiness checks pass.
2. Verify CI completed on the exact merge candidate SHA.
3. Verify `manifest.json` and `checksums.sha256`.
4. Confirm environment-specific configuration and secret references.
5. Confirm sandbox adapters are not presented as certified production connectors.
6. Confirm dashboard, alert, rollback, and on-call ownership.

## After deployment

1. Check all API health endpoints.
2. Execute approved payment smoke tests with non-production values.
3. Confirm correlation, audit, telemetry, and provider-health signals.
4. Confirm no circuit is unexpectedly open.
5. Confirm settlement and reconciliation observations.

## Incident response

Use correlation identifiers to join API, provider, audit, and settlement evidence.
Do not expose credentials or raw sensitive payloads in tickets or chat. Follow the
rollback plan when impact exceeds approved thresholds.