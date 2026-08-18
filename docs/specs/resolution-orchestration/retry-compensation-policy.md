# Retry and Compensation Policy

## Retryable

- ProviderTimeout
- ProviderTemporaryFailure

## Maximum attempts

3

## Permanent failure

`ProviderPermanentFailure` -> `FAILED`

## Retry exhaustion

-> `MANUAL_INTERVENTION_REQUIRED`

## Partial provider failure

-> `COMPENSATION_REQUIRED`

## Successful compensation

-> `COMPENSATED`

A compensated orchestration can then be finalized as `RESOLVED`.

## Financial boundary

Compensation in AFW-DLV-0018.5 is a sandbox orchestration capability. No real financial reversal is performed.
