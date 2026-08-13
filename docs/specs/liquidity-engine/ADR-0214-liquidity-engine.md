# ADR-0214 - Liquidity Management Engine

The liquidity engine reads treasury projections through `ITreasuryReadModel` and computes liquidity insights.
It remains read-only and does not create accounting entries.
