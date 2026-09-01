# AFW-MOB-BETA1.4 — Wallet Home & Balances

## Goal
Provide the Mobile Beta 1 wallet home experience using only wallet and balance data supplied by an authorized repository boundary.

## Scope
- Wallet Home screen
- Multi-wallet and multi-currency balance presentation
- Loading, empty and unavailable states
- Wallet status visibility
- Send / Receive actions visibly prepared but disabled for later deliveries
- Flutter tests and dedicated CI

## Financial boundaries
- No fabricated balance, wallet or transaction.
- No ledger mutation or money movement.
- The mobile application does not calculate an authoritative balance.
- Amounts are display projections supplied through `WalletRepository`.
- When the repository is unavailable, the UI explicitly reports that balances are unavailable.
- Send and Receive execution are outside Beta1.4.

## Freeze protocol
PR → CI → squash merge → authoritative SHA → annotated tag → remote peeled parity → DELIVERY FROZEN.

DELIVERY FROZEN: NO
