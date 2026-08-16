# Financial Risk Scoring Policy - Sandbox

## Inputs

- KYC risk: 30%
- Sanctions / PEP: 40%
- AML Monitoring: 30%

## Risk bands

- 0-29: LOW
- 30-59: MEDIUM
- 60-79: HIGH
- 80-100: CRITICAL

## Operational decision

- 0-39: ALLOW
- 40-79: REVIEW
- 80-100: RESTRICT

A confirmed sandbox sanctions block signal forces RESTRICT.

## Explainability

Every score exposes factor, raw score, weight, weighted contribution and reason. No opaque ML model is introduced.