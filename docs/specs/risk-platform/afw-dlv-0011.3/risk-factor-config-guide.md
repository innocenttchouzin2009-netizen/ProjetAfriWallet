# Risk Factor Configuration Guide

## Default weights
- fraud: 30
- aml: 25
- device: 15
- account-age: 10
- beneficiary-history: 10
- kyc: 10
- geo: 10
- behaviour: 10
- payment-type: 10

## Decision thresholds
- Allow: score < 20
- Challenge: score 20-69
- Manual review: score 70-109
- Block: score >= 110

## Notes
Weights are currently embedded in the risk weight service and can be adjusted as part of operational tuning.
