# Customer Protection Policy - Sandbox

## Repeated claims

3 or more claims may trigger a repeated-claims pattern.

## Merchant concentration

2 or more disputes linked to the same merchant may trigger merchant concentration.

## Beneficiary concentration

2 or more disputes linked to the same beneficiary may trigger beneficiary concentration.

## Failed resolutions

Failed or ManualInterventionRequired resolution states contribute to customer-protection risk.

## Slow resolution

Average investigation duration above 72 hours generates an operational-delay pattern.

## Compound risk

Three or more independent patterns add a compound-risk contribution.

## Severity

| Score | Severity |
|---|---|
| 0-9 | INFORMATIONAL |
| 10-29 | LOW |
| 30-59 | MEDIUM |
| 60-79 | HIGH |
| 80-100 | CRITICAL |

## Recommendation

| Condition | Recommendation |
|---|---|
| INFORMATIONAL | NO_ACTION |
| LOW | MONITOR |
| MEDIUM | CUSTOMER_PROTECTION_REVIEW |
| Merchant concentration with elevated score | REVIEW_MERCHANT |
| CRITICAL | ESCALATE_OPERATIONS |

These are sandbox policy thresholds and are not regulatory thresholds.
