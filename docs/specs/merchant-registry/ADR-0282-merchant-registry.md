# ADR-0282

## Merchant Registry & Business Profile Platform

### Status

Accepted for AFW-DLV-0019.1.

### Context

AfriWallet requires a canonical merchant identity before merchant verification, checkout, acceptance and payout capabilities can be built.

### Decision

Create a dedicated Merchant Registry bounded context.

### Merchant identity

Every merchant receives an immutable identifier: `AFM-<random identifier>`

The identifier is independent from:

- AWID
- bank account number
- tax identifier
- payment provider account
- checkout identifier

### Owner identity

A merchant may reference an AfriWallet owner through AWID. The Merchant Registry does not modify the identity platform.

### Business profile

The registry stores descriptive business information. It does not certify that the information has been independently verified.

### Status boundary

`Active` is an administrative Merchant Registry status. It does not authorize payment processing.

### Financial boundary

No method in AFW-DLV-0019.1 may:

- accept payments
- authorize payments
- capture payments
- settle funds
- perform payouts
- mutate Universal Ledger
