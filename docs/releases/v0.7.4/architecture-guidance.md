# Architecture Guidance — Payment Gateway Layer

## Vision
AfriWallet évolue d'une collection de modules de paiement indépendants vers une plateforme modulaire de paiements.

## Proposed architecture
```
Payment Gateway
├── Mobile Money
│   ├── MTN
│   ├── Orange
│   ├── Moov
│   └── Airtel
├── Banking
│   ├── SEPA
│   ├── Local Banks
│   └── SWIFT
└── Cards
    ├── Visa
    ├── Mastercard
    └── Virtual Cards
```

## Shared platform services
- Configuration and secrets
- Audit trail
- Correlation and structured logging
- Resilience and rate limiting
- Metrics and tracing
- Feature flags
- Validation and release packaging

## Expected impact
- Less duplication across payment providers
- Faster onboarding of new partners
- Consistent enterprise controls across all payment rails
