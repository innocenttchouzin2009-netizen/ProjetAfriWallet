# Banking Platform Security Boundary

## Required controls
- No production bank credentials in Git.
- No private signing keys in Git.
- No production bank endpoint enabled by default.
- Webhooks require cryptographic verification.
- Idempotency is mandatory for transfer execution.
- Provider errors must not mutate historical ledger data.
- Financial reconciliation discrepancies must remain explicit.
- Provider adapters must remain isolated from core domain state.

## Current environment
SANDBOX ONLY

## Production certification
NOT CLAIMED
