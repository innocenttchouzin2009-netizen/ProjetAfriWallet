# ADR-0219 - Matching and Exception Strategy

Matching strategy:
- Exact: partner, currency, reference, amount, and time-window all compatible
- Partial: partner, currency, and reference match but amount/time differs
- Unmatched: no functional counterpart

Exception strategy:
- EXTERNAL_RECORD_MISSING when no external counterpart exists
- INTERNAL_RECORD_MISSING when an external orphan remains
- RECONCILIATION_DIFFERENCE when partial matches are detected

Exceptions are first-class artifacts for operations and audit review.
