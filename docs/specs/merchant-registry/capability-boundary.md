# Merchant Capability Boundary

The Merchant Registry may store declared capabilities:

- InPersonPayments
- OnlinePayments
- QrPayments
- PaymentLinks
- Refunds
- Payouts

These values represent intended or configured merchant capabilities. They do not prove:

- KYB approval
- payment-provider onboarding
- card-scheme activation
- settlement availability
- payout availability

Downstream deliveries must independently verify whether a capability is operationally enabled.
