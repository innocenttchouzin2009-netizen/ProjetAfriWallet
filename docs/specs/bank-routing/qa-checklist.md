# QA checklist

- DE/EUR with eligible SEPA Instant selects SepaInstant.
- NG/NGN selects LocalBankTransfer.
- Ineligible rails are filtered before scoring.
- Unhealthy and inactive rails are excluded.
- Min/max amount constraints are enforced.
- Idempotency reuses the original routing decision.
- Reason field explains the selection deterministically.
- Fallback rails remain ordered and bounded.
- No bank order is issued from the routing engine.
