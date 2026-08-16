# AML Monitoring Policy - Sandbox

## Large amount

Detect transactions exceeding configured sandbox thresholds.

## Velocity

Detect unusually high transaction frequency within a short period.

## Structuring

Detect repeated amounts clustered below a monitoring threshold.

## Geographic risk

Use synthetic sandbox geography codes `XZ` and `XY` only.

## Repeated beneficiary

Detect repeated transfers toward the same beneficiary within a short window.

## Explainability

Every evaluation exposes rule code, rule type, reason and risk points. No opaque machine-learning decision is introduced.