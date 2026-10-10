# Financial Timeline Specification

## Intent

Financial Timeline is a user-facing projection layer for wallet activity. It is not the accounting source of truth.

- Ledger: immutable financial truth
- Financial Timeline: UX-oriented representation of financial events

## Example

Aujourd'hui

- +250 EUR Salaire 08:00
- -50 EUR Restaurant 12:45
- +120 EUR Transfert 17:10

## Rules

- Every timeline event must reference one or more ledger transaction IDs.
- Timeline ordering is by business timestamp descending.
- Timeline labels and icons are presentation concerns and must not alter ledger semantics.
- Deleting a timeline item does not delete underlying ledger entries.

## Future

The timeline detail view (planned for AFW-DLV-0004.5) may expose linked metadata such as merchant, category, and dispute status while preserving traceability back to ledger transactions.
