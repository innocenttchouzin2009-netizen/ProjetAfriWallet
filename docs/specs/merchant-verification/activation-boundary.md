# Merchant Verification / Payment Activation Boundary

Merchant verification does not enable commerce execution.

The following remain false in AFW-DLV-0019.2:

- `paymentAcceptanceEnabled = false`
- `captureEnabled = false`
- `settlementEnabled = false`
- `payoutEnabled = false`
- `moneyMovementPerformed = false`
- `ledgerMutationPerformed = false`

Payment acceptance requires a separately governed Sprint 19 delivery.
