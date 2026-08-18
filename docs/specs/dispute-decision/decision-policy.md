# AFW Dispute Resolution Policy v1.0

## EvidenceDoesNotSupportClaim

Decision: `DECLINE`

## InsufficientEvidence

Decision: `MANUAL_REVIEW`

## ManualEscalationRequired

Decision: `MANUAL_REVIEW`

## EvidenceSupportsClaim + UnauthorizedTransaction

Decision: `CHARGEBACK_RECOMMENDED`

## EvidenceSupportsClaim + DuplicateTransaction

Decision: `REFUND_RECOMMENDED`

## EvidenceSupportsClaim + ProcessingError

Decision: `REFUND_RECOMMENDED`

## EvidenceSupportsClaim + RefundNotProcessed

Decision: `CHARGEBACK_RECOMMENDED`

## High-value threshold

Disputed amount >= 1000 requires manual approval.

## Unknown classification

Decision: `MANUAL_REVIEW`
