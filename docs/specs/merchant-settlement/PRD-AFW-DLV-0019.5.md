# AFW-DLV-0019.5

Orchestrate settlement and payout workflows from approved `CaptureEligible` decisions using a sandbox provider only. It supports idempotency, provider correlation, retries, partial failure compensation, terminal protection and audit.

No real capture, settlement, payout, customer/merchant funds movement, wallet mutation or direct Universal Ledger write occurs. `Completed` means the sandbox orchestration workflow completed, not that merchant funds settled.
