# ADR-0036 — Currency Precision

## Status
Accepted

## Context
Financial amounts must remain exact and explicit. Using floating-point types for money can lead to rounding errors and inconsistent behavior.

## Decision
All monetary values are represented in minor units. Conversion from major units to minor units uses decimal arithmetic with an explicit rounding policy via MidpointRounding.ToEven.

## Consequences
- Monetary calculations are deterministic.
- The system avoids float and double for financial values.
- Currency precision is explicit and testable.
