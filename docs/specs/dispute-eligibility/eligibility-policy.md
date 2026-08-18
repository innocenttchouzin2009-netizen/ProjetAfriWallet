# Eligibility Policy - Sandbox

| Rule | Requirement |
|---|---|
| `DSP-ELG-000` | Referenced transaction must exist |
| `DSP-ELG-001` | Claim AWID must match transaction AWID |
| `DSP-ELG-002` | Claim currency must match transaction currency |
| `DSP-ELG-003` | Claim amount must be positive and not exceed the transaction amount |
| `DSP-ELG-004` | Claim must be submitted within 120 days of the transaction |
| `DSP-ELG-005` | Transaction must be `Completed` or `Settled` |
| `DSP-ELG-006` | Claim type must be specific enough to classify automatically |

A claim typed `Other` always yields `ManualReviewRequired`. Any other failing rule yields `Ineligible` with the first matching primary reason.

The 120-day window is sandbox policy and is not calibrated against any specific scheme or regulator.
