# AFW-DLV-0018.6 Execution Boundary

The Customer Protection & Dispute Intelligence Engine is analytical. It must not directly:

- disable merchants
- suspend customers
- freeze wallets
- execute refunds
- submit chargebacks
- create financial ledger postings
- mutate payment settlement
- perform financial recovery

Recommendations require separately governed downstream action. Every audit event records explicit `false` proofs for `automaticMerchantBlockingPerformed`, `automaticCustomerSuspensionPerformed`, `refundExecutionPerformed`, `moneyMovementPerformed`, and `ledgerMutationPerformed`.
