# ADR-0271 - Fraud Intelligence & Pattern Correlation

## Status

Accepted for AFW-DLV-0017.6.

## Decision

Introduce a dedicated Fraud Intelligence bounded context consuming normalized snapshots rather than coupling directly to the internal implementations of earlier fraud engines.

The first release uses deterministic rules only. Every score contribution maps to an explicit `FraudPattern`, so a finding can be reconstructed from its patterns.

## Enforcement boundary

Intelligence findings are observational artifacts. They do not execute payment, account, wallet, or device mutations.