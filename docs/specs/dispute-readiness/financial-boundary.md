# Dispute Platform Financial Boundary

Sprint 18 separates decision from orchestration from real financial execution.

AFW-DLV-0018.4 may recommend:

- REFUND
- CHARGEBACK

AFW-DLV-0018.5 may orchestrate those decisions using sandbox providers. Neither delivery performs actual financial settlement.

The platform must not directly:

- credit wallet balances
- debit wallet balances
- execute real refunds
- submit real chargebacks
- move merchant funds
- write Universal Ledger transactions

Any future production financial execution requires a separately governed delivery.
