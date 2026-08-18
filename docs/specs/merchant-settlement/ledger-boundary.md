# AFW-DLV-0019.5 Ledger Boundary

Merchant Settlement tracks amount, currency, provider reference, attempts, compensation and completion state. It cannot create/reverse ledger postings, debit customer wallets, credit merchant wallets or modify balances. Audit flags remain false for real capture, settlement, payout, funds movement, wallet mutation and direct ledger mutation.
