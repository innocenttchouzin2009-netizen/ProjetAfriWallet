# AFW-DLV-0019.1

## Merchant Registry & Business Profile Platform

### Objective

Provide the canonical merchant identity and business profile registry for the AfriWallet Merchant Platform.

### Capabilities

- permanent AfriWallet merchant identifier
- owner AWID association
- legal business profile
- trading name
- merchant type
- business category
- country
- settlement currency declaration
- business address
- contact details
- registration and tax references
- declared merchant capabilities
- lifecycle management
- audit trail
- immutable closed state

### Merchant lifecycle

Draft -> Registered -> PendingVerification -> Active

Active -> Suspended -> Active

Any mutable state -> Closed

Closed is terminal.

### Critical boundary

AFW-DLV-0019.1 does not perform:

- KYB verification
- merchant underwriting
- payment authorization
- payment acceptance
- payment capture
- settlement
- payout
- money movement
- Universal Ledger mutation

### Capability semantics

Merchant capabilities are declarations only. For example, `OnlinePayments` does not mean that online payment acceptance is activated.

### Validation

Successful validation means: `READY FOR REVIEW`
