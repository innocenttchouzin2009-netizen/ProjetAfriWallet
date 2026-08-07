# AFW-DLV-0013.5 Accounting and General Ledger

This delivery adds the technical accounting layer for AfriWallet.

## Scope

- General ledger accounts
- Accounting periods with open/closed protection
- Balanced journal entry posting
- Reversal generation
- Trial balance projection

## Runtime Surface

- `POST /api/v1/accounting/accounts`
- `POST /api/v1/accounting/periods`
- `POST /api/v1/accounting/journal-entries`
- `POST /api/v1/accounting/journal-entries/{journalEntryId}/reverse`
- `GET /api/v1/accounting/periods/{periodId}/trial-balance`

## Invariants

- Posted journal entries are immutable.
- Journal entries must balance before posting.
- A journal entry can only post into an open accounting period.
- Reversals keep the source journal entry identifier for auditability.