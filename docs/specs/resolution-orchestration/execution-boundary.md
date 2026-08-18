# Execution Boundary

AFW-DLV-0018.5 orchestrates resolution workflows against a sandbox-only provider.

It must not:

- execute a real refund
- submit a real chargeback
- move customer or merchant money
- mutate wallet balances
- write directly to Universal Ledger
- perform external settlement

Every audit event records explicit `false` proofs for `realRefundPerformed`, `realChargebackSubmitted`, `realMoneyMovementPerformed`, `directLedgerMutationPerformed`, and `externalProviderSettlementPerformed`.
