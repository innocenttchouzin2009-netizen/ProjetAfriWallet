# Checkout Idempotency Policy

Every Payment Intent needs a non-empty idempotency key. Within the sandbox repository, the same key returns the same Payment Intent and Checkout Session. Merchant order reference additionally prevents accidental duplicate checkout creation for the same merchant order. Idempotency never authorizes or executes payment.
