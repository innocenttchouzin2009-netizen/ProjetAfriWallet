# Operations Guide — Transfer Execution Engine

## Overview
The transfer execution engine executes validated transfer intents through a connector-aware pipeline.

## Responsibilities
- Resolve connectors automatically.
- Track execution state.
- Support retries and cancellation.
- Surface execution records for audit and monitoring.

## Monitoring
- Watch the health endpoint for service availability.
- Review execution records via the execution listing endpoint.
