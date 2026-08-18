# AFW-DLV-0019.2

## Merchant Onboarding & Verification Orchestration

### Objective

Orchestrate merchant onboarding and sandbox verification for merchants registered by AFW-DLV-0019.1.

### Inputs

- MerchantId
- Owner AWID
- merchant profile snapshot
- verification documents

### Capabilities

- verification case creation
- document collection
- minimum sandbox document policy
- SHA-256 duplicate document prevention
- reviewer assignment
- manual notes
- sandbox provider verification
- Verified
- Rejected
- ManualReviewRequired
- audit trail
- immutable terminal decisions

### Important semantic boundary

`Verified` means the merchant passed the sandbox verification workflow implemented by AFW-DLV-0019.2.

`Verified` does not mean:

- regulator approved
- bank approved
- card scheme approved
- payment acceptance enabled
- settlement enabled
- payout enabled

### Financial boundary

AFW-DLV-0019.2 does not:

- accept payments
- capture payments
- settle funds
- execute payouts
- move money
- mutate Universal Ledger

### Validation

Successful validation means: `READY FOR REVIEW`
