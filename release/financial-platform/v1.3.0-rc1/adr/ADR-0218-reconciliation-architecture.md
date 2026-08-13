# ADR-0218 - Reconciliation Architecture

Reconciliation is implemented as a dedicated bounded context with Domain, Application, Infrastructure, Contracts, API, and Scenarios.

The run orchestrator reads both internal and external records from the data source abstraction, computes matches through a dedicated matcher, records exceptions, and stores immutable run outcomes for auditability.
