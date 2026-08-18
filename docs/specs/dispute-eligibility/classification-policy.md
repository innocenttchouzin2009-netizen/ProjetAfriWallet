# Classification Policy - Sandbox

| Claim type | Category |
|---|---|
| `TransactionNotRecognized` | `UnauthorizedTransaction` |
| `DuplicateCharge`, `WrongAmount` | `ProcessingError` |
| `ServiceNotReceived`, `GoodsNotReceived`, `MerchantDispute` | `MerchantService` |
| `RefundNotReceived` | `RefundIssue` |
| `CashWithdrawalDispute` | `CashWithdrawal` |
| `BankTransferDispute` | `BankTransfer` |
| `FraudRelated` | `FraudRelated` |
| `Other` | `Other` |

Classification is deterministic and independent of eligibility: an ineligible claim is still classified so downstream deliveries keep explainable context.

A `FraudRelated` classification records fraud context only; it is not a fraud determination and not a dispute decision.
