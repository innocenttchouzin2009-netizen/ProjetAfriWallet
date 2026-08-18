# AFW-DLV-0018.4

## Refund & Chargeback Decision Engine

### Objective

Transform completed dispute investigations into deterministic, versioned, explainable and auditable resolution decisions.

### Inputs

- AFW-DLV-0018.1 - Dispute Case & Claim Registry
- AFW-DLV-0018.2 - Eligibility & Classification
- AFW-DLV-0018.3 - Evidence & Investigation Platform

### Decisions

The engine may produce:

- RefundRecommended
- ChargebackRecommended
- Decline
- ManualReview

### Capabilities

- deterministic policy evaluation
- policy versioning
- reason codes
- decision factors
- idempotent evaluation
- controlled reevaluation
- superseded decision history
- manual approval
- audit trail

### Critical financial boundary

This delivery does not:

- execute refunds
- submit chargebacks
- recover merchant funds
- debit wallets
- credit wallets
- create ledger entries
- reverse ledger entries
- mutate payment settlement

A recommendation is not a financial execution instruction.

### Validation decision

Successful validation means: `READY FOR REVIEW`

It does not mean: `DELIVERY FROZEN`
