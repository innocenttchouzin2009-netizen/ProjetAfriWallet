# AFW-MOB-BETA1.5 — Send & Receive

## Goal
Provide the first mobile Send & Receive experience without fabricating financial state.

## Send
- Collect recipient identifier / AWID, amount and currency.
- Submit only through `TransferRepository`.
- Display only the status returned by the repository/backend.
- Production fallback is unavailable and never simulates success.

## Receive
- Display only a public receive identity supplied by `TransferRepository`.
- Display QR availability only when a backend QR token is actually supplied.
- Never generate a fake payment QR.

## Backend alignment
The existing Payment Intent platform exposes creation and lifecycle endpoints. Beta1.5 keeps Flutter behind a repository boundary so a real HTTP adapter can bind to those contracts without moving payment authority into the client.

## Financial boundaries
- No local ledger mutation.
- No locally authoritative balance update.
- No fake transfer success.
- No fake recipient resolution.
- No fake QR/payment token.
- Idempotency is required for send requests.
- Backend/provider confirmation remains authoritative.

## Freeze protocol
Implementation -> tests -> CI -> PR -> squash merge -> authoritative SHA -> annotated tag -> local/remote peeled SHA parity.

DELIVERY FROZEN: NO
