# PRD — AFW-DLV-0019.8 — Merchant Platform Release Candidate v1.9.0-rc1

## Objective
Produce the auditable release candidate that closes Sprint 19 after AFW-DLV-0019.7 production readiness is frozen.

## Required gates
1. Branch starts from the authoritative frozen 0019.7 SHA.
2. `sprint19-dlv-0019.7` exists and resolves to that SHA.
3. RC manifest and release notes identify deliveries 0019.1–0019.7.
4. Existing Merchant readiness validation remains green.
5. No automatic merchant blocking/suspension is introduced.
6. No automatic settlement or payout freeze is introduced.
7. No payment capture, money movement, or ledger mutation is introduced by RC packaging.
8. Secret scan and `git diff --check` pass.

## Candidate tag
`merchant-platform-v1.9.0-rc1`

The candidate tag MUST NOT be created before CI success, squash merge, authoritative main SHA verification, and peeled SHA parity verification.

## Sprint closure
Sprint 19 is closed only after this delivery is officially frozen.
