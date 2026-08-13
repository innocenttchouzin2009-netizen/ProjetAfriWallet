# PRD AFW-DLV-0014.3 — Merchant Acquiring Platform

## Scope
Merchant Acquiring is responsible for accepting merchant payments, profile activation, fee configuration, capture, refunds and settlement preparation.

It does not own merchant identity or KYC. Merchant Registry / KYC remains the source of truth from AFW-DLV-0009.x.

```
Merchant Registry / KYC
        ↓
AFW-DLV-0009.x

Merchant Acquiring
        ↓
AFW-DLV-0014.3
```

## Responsibilities
- Accept merchant payment intent and validate eligibility
- Manage acquiring profiles and authorized payment methods
- Configure fees and settlement currency
- Route accepted payments through Payment Routing Engine
- Capture and refund authorized payments
- Prepare settlement accounting data

## Non-goals
- Merchant KYC or registry maintenance
- Payment execution beyond acquiring orchestration
- Duplicate payment routing engine behavior
