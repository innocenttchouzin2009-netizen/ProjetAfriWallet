# ADR-0286 - Merchant Settlement Payout Orchestration

A dedicated Merchant Settlement context consumes `CaptureEligible` decisions without coupling policy decision to financial execution. A decision creates only one active orchestration; idempotency keys are mandatory. Temporary failure/timeouts retry at most three times; exhaustion requires manual intervention. Partial sandbox processing requires compensation. No real funds move and no direct Universal Ledger writer exists.
