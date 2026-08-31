# ADR-0171 — Merchant Platform Readiness Gates

Status: Proposed by AFW-DLV-0019.7

## Decision
Merchant RC promotion is guarded by deterministic repository-level readiness checks covering delivery presence, frozen-tag evidence, architecture boundaries, financial non-execution boundaries, deterministic intelligence evidence, release tooling and documentation.

The readiness layer is verification-only. It MUST NOT block/suspend merchants, freeze settlement/payout, capture payments, move money, or mutate the Universal Ledger.

## Rationale
Production readiness must be independently repeatable in Codespaces and CI, fail closed when evidence is missing, and preserve the execution boundaries established by frozen Merchant Platform deliveries.

## Consequence
AFW-DLV-0019.8 may start only after 0019.7 is merged, CI-successful, tagged on its authoritative squash SHA, and frozen.