# ADR-0284 - Checkout Payment Intent

## Decision

AFW-DLV-0019.3 introduces `CheckoutSession` for merchant/customer commerce context and `PaymentIntent` for requested payment intent. Neither represents a completed financial transaction.

## Idempotency

A payment-intent idempotency key returns the same intent and checkout. Merchant order reference also prevents duplicate checkout creation for the same merchant order.

## Boundary

Only payment-method token references are stored. `ReadyForAuthorization` does not mean authorized, captured, paid or settled. No Universal Ledger writer is introduced.
