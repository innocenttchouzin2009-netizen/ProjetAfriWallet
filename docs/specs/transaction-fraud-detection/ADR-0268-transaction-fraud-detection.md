# ADR-0268 — Transaction Fraud Detection Engine

## Status
Accepted for AFW-DLV-0017.3.

## Context
Transaction-fraud detection requires evidence from multiple fraud sources without coupling directly to their internal aggregates.

## Decision
0017.3 consumes normalized snapshots from:
- Fraud Signal Platform
- Device & Account Risk Engine

It produces its own immutable detection result.

## Explainability
Every positive factor contains:
- factor type
- score contribution
- reason
- optional evidence identifier

## Non-execution boundary
The engine produces recommendations only.
Payment authorization and blocking remain outside AFW-DLV-0017.3.

## Future
AFW-DLV-0017.4 may consume the result of this engine as one input to a formal Fraud Rules & Decision Engine.
