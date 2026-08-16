# ADR-0261 - AML Transaction Monitoring

## Status

Accepted for AFW-DLV-0016.4.

## Decision

Transaction monitoring uses deterministic, explainable rule evaluators behind application abstractions. Enabled rules contribute bounded risk points and can generate investigative alerts.

## Data boundary

Only normalized transaction metadata required by the configured rules is retained in the sandbox history repository.

## Regulatory boundary

The engine does not file reports, suspend accounts or make legal determinations. Production policy activation requires a separate controlled delivery.

## Relationship to screening

Sanctions and PEP screening from AFW-DLV-0016.3 remains a separate bounded capability and is not reimplemented here.