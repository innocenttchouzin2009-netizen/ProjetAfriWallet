# Payment Platform Observability Guide

## Required signals

- health endpoint availability for all six APIs
- correlation identifiers across provider execution
- auditable payment lifecycle events
- operational telemetry for initiation, status, callbacks, and providers
- provider success rate and average latency
- retry, circuit-open, failure, and recovery outcomes

## Dashboard baseline

The Payment RC dashboard must show:

- request rate and error rate by API
- latency percentiles by provider and operation
- provider success rate
- retry count and circuit-open count
- webhook verification failures
- idempotency conflicts
- settlement and reconciliation failures

## Alert baseline

- provider success rate below 95 percent for five minutes
- circuit open for any enabled provider
- webhook verification failure spike
- payment or settlement error rate above the approved threshold
- readiness or dependency scan failure in CI

Production alert thresholds must be approved against expected traffic before
provider activation.