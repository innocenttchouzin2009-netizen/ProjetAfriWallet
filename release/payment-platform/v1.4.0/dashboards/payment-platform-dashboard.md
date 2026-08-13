# Payment Platform Dashboard Specification

Required views:

- API request rate, error rate, and latency percentiles
- provider success rate and average latency
- retry and circuit-open counts
- webhook verification failures
- idempotency conflicts
- acquiring, settlement, and reconciliation failures
- release validation and dependency scan status

Alert when provider success rate drops below the approved threshold or any
enabled provider circuit opens.