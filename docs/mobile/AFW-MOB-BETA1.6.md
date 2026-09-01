# AFW-MOB-BETA1.6 — Transactions / Transaction History

## Goal
Provide a trustworthy mobile transaction-history experience without making the mobile client a financial source of truth.

## Scope
- Transaction history domain model.
- Repository boundary for backend-provided history.
- Incoming/outgoing direction.
- Amounts represented in integer minor units.
- Currency, status, timestamp, reference and counterparty label.
- Transaction list and detail experience.
- Explicit empty and unavailable states.
- Flutter tests and dedicated CI.

## Backend audit
The current Accounting API exposes account creation, accounting-period creation, journal-entry posting/reversal and trial-balance retrieval. It does not expose a customer-facing transaction-history read endpoint suitable for direct mobile consumption.

Therefore Beta1.6 does not derive customer transaction history from the General Ledger and does not fabricate a production adapter. A future backend read model/API can implement `TransactionHistoryRepository` without changing the UI contract.

## Financial boundaries
- No fabricated production transaction history.
- No local ledger reconstruction.
- No local balance mutation.
- No inference that an accounting journal line is automatically a customer-visible transaction.
- Integer minor units only for monetary amounts.
- Backend financial records remain authoritative.

## Freeze protocol
Do not create or move `mobile-beta1-dlv-beta1.6` until PR CI is green, squash merge is complete, the authoritative squash SHA is verified in `origin/main`, and local/remote peeled tag parity is confirmed.

DELIVERY FROZEN: NO
