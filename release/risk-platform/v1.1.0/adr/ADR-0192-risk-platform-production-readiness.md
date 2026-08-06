# ADR-0192 — Risk Platform Production Readiness

## Status
Accepted

## Context
AFW-DLV-0011.1 to AFW-DLV-0011.6 deliver core risk capabilities but require a single production-readiness gate that validates integration quality, operational controls, and release evidence before enterprise rollout.

## Decision
Implement a dedicated readiness stream (AFW-DLV-0011.7) with:
- A single script-driven validation runner (`validate-risk-platform.ps1`).
- Deterministic pass/fail checks for configuration, security, resilience, observability, and functional modules.
- Mandatory release package generation in `release/risk-platform/v1.1.0`.

## Consequences
- Positive: repeatable release qualification and auditable evidence.
- Positive: reduced manual review burden through standardized reports and checksums.
- Trade-off: stricter gating may block releases until all controls are green.
