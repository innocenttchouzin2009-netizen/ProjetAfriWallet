# AFW-DLV-0017.3 — Transaction Fraud Detection Engine

## Inputs
- AFW-DLV-0017.1 Fraud Signal & Event Platform
- AFW-DLV-0017.2 Device & Account Risk Engine

## Detection factors
- unusual amount
- newly added beneficiary
- high transaction velocity
- recent device change
- device/account risk
- failure followed by subsequent payment attempt
- geographic anomaly
- repeated transaction attempts

## Outputs
- score
- risk band
- detection factors
- evidence references
- operational recommendation
- audit record

## Recommendations
- ALLOW
- REVIEW
- CHALLENGE
- DECLINE_RECOMMENDED

## Critical boundary
DECLINE_RECOMMENDED is not an execution command.
AFW-DLV-0017.3 does not mutate payment state.

## No duplication
0017.3 must not duplicate:
- fraud signal ingestion
- device/account scoring
- AML monitoring
- KYC verification
- sanctions screening

## Production policy
Detection weights and thresholds remain development policy until formally calibrated.

## Decision
Successful validation means: READY FOR REVIEW.
It does not mean: PAYMENT DECLINED, ACCOUNT SUSPENDED, FRAUD LEGALLY CONFIRMED.
