# Routing Policy

1. Reject rails that are inactive or unhealthy.
2. Reject rails that do not match country and currency.
3. Reject rails that violate min/max amount boundaries.
4. Prefer the highest-priority eligible rail.
5. Prefer SEPA Instant when it is eligible and healthy.
6. Use deterministic cost as a tiebreaker.
7. Keep a bounded ordered fallback list.
8. Record the selection reason and explicit fallback rails.
