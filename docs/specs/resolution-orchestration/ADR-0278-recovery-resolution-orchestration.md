# ADR-0278 - Recovery & Resolution Orchestration Platform

## Status

Accepted for AFW-DLV-0018.5.

## Context

AFW-DLV-0018.4 creates approved refund or chargeback decisions. A separate orchestration layer is required to manage provider delivery, retries, failures and compensation.

## Decision

Introduce a dedicated Resolution Orchestration bounded context.

## Idempotency

Every orchestration requires an idempotency key. The same decision cannot create multiple active orchestrations.

## Retry

Transient provider failures may be retried. Maximum attempts: 3. After exhaustion, the orchestration enters: `ManualInterventionRequired`.

## Compensation

Partial provider processing triggers: `CompensationRequired`. Successful compensation transitions to: `Compensated`.

## Ledger boundary

The orchestration component does not write directly to Universal Ledger.

## Provider boundary

The AFW-DLV-0018.5 provider is sandbox-only. No claim of live processor, bank, card-scheme or settlement integration is made by this delivery.
