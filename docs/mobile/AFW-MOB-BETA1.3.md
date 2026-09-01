# AFW-MOB-BETA1.3 — Identity & AfWal ID

## Objective
Deliver the Mobile Beta 1 identity experience around the permanent AfWal ID without inventing identity or financial state.

## Scope
- AfWal ID profile screen
- public alias or permanent AWID display when supplied by the identity layer
- privacy-state display
- copy/share-ready public identity interaction
- QR placeholder that never fabricates a backend QR token
- explicit unavailable/error state
- clean repository boundary for future authenticated backend integration
- widget tests and CI validation

## Security and product boundaries
- Production UI must not create a fake AWID, alias, balance, transaction, or QR token.
- A QR is displayed as actionable only when a valid backend-issued token exists.
- AfWal ID is a public identity and does not grant access to wallet funds.
- Authentication/session integration remains owned by the existing identity service and later mobile integration work.

## Validation
- `flutter pub get`
- `flutter analyze`
- `flutter test`
- `git diff --check HEAD^ HEAD`

DELIVERY FROZEN: NO
