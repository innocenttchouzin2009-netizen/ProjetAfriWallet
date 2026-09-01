# AFW-MOB-BETA1.2 — Onboarding & Authentication Experience

## Objective
Turn the frozen Mobile Beta 1 design foundation into the first guided AfWal account-access journey.

## Included
- Three-step premium onboarding journey.
- AfWal brand and security positioning.
- Create-account and sign-in entry points.
- Country selection for account creation.
- Phone/e-mail and PIN form validation.
- Navigation into the existing Beta experience.
- Explicit UI messaging that backend authentication remains the source of truth.

## Financial and security boundaries
- No money movement is introduced.
- No wallet or ledger mutation is introduced.
- No balance or transaction is fabricated.
- No client-side form result is treated as authenticated identity.
- Real OTP, PIN verification, device trust and sessions must be provided by validated AfWal backend services before production authentication is enabled.

## Validation gate
The Mobile Beta workflow must run Flutter dependency resolution, static analysis, tests and diff validation before merge.

## Freeze protocol
DELIVERY FROZEN: NO

Freeze only after required CI succeeds, the PR is squash-merged, the authoritative squash SHA is verified in `origin/main`, and the annotated delivery tag has local/remote peeled SHA parity.
