# ADR-0269 - Fraud Rules & Decision Engine

## Status

Accepted for AFW-DLV-0017.4.

## Decision

Introduce a Fraud Decision bounded context consuming normalized snapshots from the Device Risk and Transaction Fraud engines.

## Combination policy

- Transaction fraud score: 65%.
- Device and account risk score: 35%.

If transaction fraud is at least 90 and device risk is at least 80, the engine applies a critical override, forcing score 100 and `DECLINE_RECOMMENDED`.

Every decision stores rule evaluations and reasons. The engine has no payment execution dependency.