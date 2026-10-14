# AfWal Group Collection / Shared Payment — Frozen Product Concept

Status: **OFFICIALLY ADOPTED — FROZEN / DEFERRED**

Date frozen: 2026-10-15

## Product intent

AfrikaWallet will support a future multi-payer collection flow where one organizer creates a target amount for a beneficiary and invites several participants to contribute toward the same goal.

Example:
- beneficiary: Henry in Cameroon
- target: 1,000,000 XAF
- contributors: 4 participants
- each participant can join from an invitation and fund their contribution
- progress is visible until the target is reached
- payout/release behavior will be defined in the future implementation phase

## Future experience

Create collection -> define target -> select beneficiary -> invite participants -> contribute -> show progress -> target reached -> release/pay beneficiary.

Potential contribution rails may later include:
- AfWal wallet balance
- AfWal ID
- QR / shareable invitation link
- SEPA
- Apple Pay / Google Pay
- Mobile Money where supported

## Product boundary

This capability is distinct from AfriCircle:
- **AfriCircle**: recurring/community group saving and contribution model.
- **AfWal Group Collection / Shared Payment**: simple, goal-based, usually one-off multi-payer collection.

## Freeze decision

This concept is intentionally **not part of the active delivery line**.

Do not assign a Mobile Beta or backend delivery number, create implementation branches, or expand runtime scope until the feature is explicitly unfrozen by product decision.

Current engineering priority remains the main AfrikaWallet delivery line: authenticated mobile experience, P2P, Mobile Money payout, funding rails, production security/compliance, virtual card, and premium UI convergence.

No code, ledger behavior, transfer behavior, payout behavior, or API contract is changed by this document.
