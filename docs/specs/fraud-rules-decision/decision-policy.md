# Fraud Decision Policy - Sandbox

## Weighted score

`transaction fraud * 0.65 + device risk * 0.35`, rounded to the nearest integer and clamped to 0-100.

## Bands

- 0-29: `LOW`
- 30-59: `MEDIUM`
- 60-79: `HIGH`
- 80-100: `CRITICAL`

## Actions

- 0-29: `ALLOW`
- 30-59: `REVIEW`
- 60-79: `CHALLENGE`
- 80-100: `DECLINE_RECOMMENDED`

## Critical override

Transaction fraud >= 90 and device risk >= 80 produces score 100, `CRITICAL`, and `DECLINE_RECOMMENDED`.

All weights and thresholds remain sandbox/development policy until formally calibrated.