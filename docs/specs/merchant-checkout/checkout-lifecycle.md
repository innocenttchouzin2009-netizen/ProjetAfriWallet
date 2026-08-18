# Checkout Session Lifecycle

`Open -> ReadyForPayment | Cancelled | Expired`

`Cancelled` and `Expired` are terminal. `ReadyForPayment` means the linked Payment Intent is `ReadyForAuthorization`; no payment has been authorized.
