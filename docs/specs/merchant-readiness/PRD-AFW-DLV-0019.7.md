# AFW-DLV-0019.7 — Merchant Platform Production Readiness

## Objective
Prove that Merchant Platform deliveries 0019.1 through 0019.6 are structurally present, release-gated, auditable, deterministic where intelligence is involved, and safe to promote to the Merchant Platform release-candidate delivery.

## Readiness gates
1. Merchant Registry, Onboarding, Checkout, Payment Decision, Settlement and Intelligence are present.
2. Frozen delivery tags `sprint19-dlv-0019.1` through `sprint19-dlv-0019.6` are available to the validation checkout.
3. Merchant source must not directly depend on ledger infrastructure.
4. Merchant intelligence remains deterministic/explainable.
5. Secret scanning and `git diff --check` are mandatory CI gates.
6. Any failed readiness check blocks RC promotion.

## Non-negotiable boundaries
- Automatic merchant blocking: NOT IMPLEMENTED.
- Automatic merchant suspension: NOT IMPLEMENTED.
- Automatic settlement freeze: NOT IMPLEMENTED.
- Automatic payout freeze: NOT IMPLEMENTED.
- Payment capture: NOT IMPLEMENTED.
- Money movement: NOT IMPLEMENTED.
- Ledger mutation: NOT IMPLEMENTED.

Readiness evaluates and reports; it does not execute financial actions or enforcement.

## Validation
Run `./tools/release/validate-merchant-readiness.ps1 -Configuration Release`.
A successful run must end with `Decision: READY FOR MERCHANT RC` and exit code 0.

## Freeze protocol
After PR CI is fully successful: squash merge, retrieve the authoritative squash SHA from GitHub, verify it is contained in `origin/main`, create annotated tag `sprint19-dlv-0019.7` on exactly that SHA, push once, then verify local and remote peeled SHA parity. Historical tags must never be moved, recreated or force-pushed.