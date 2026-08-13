# QA checklist

- Intent creation succeeds with valid inputs.
- Duplicate idempotency keys return the original intent.
- Missing beneficiary or inactive beneficiary is rejected.
- Unverified bank account is rejected.
- Currency mismatch is rejected.
- Confirmed transfer can progress to ready-for-routing.
- Invalid lifecycle transitions fail fast.
- Cancellation works before finalization.
- Expiration is enforced before route handoff.
