# ADR-0057: Idempotent Transfer Execution

## Status
Accepted

## Context
Repeated transfer execution requests for the same payment intent must not cause duplicate ledger transactions or duplicate transfer records.

## Decision
The transfer engine will use the payment intent as the idempotency anchor. If a transfer already exists for the intent, subsequent requests will return the existing transfer rather than create a second ledger transaction.

## Consequences
- Duplicate execution requests are safe.
- Ledger integrity is preserved.
- Clients can safely retry transfers without creating duplicates.
