# Rollback Plan

## Trigger

Any validation failure, checksum mismatch, or missing release artifact invokes rollback preparation.

## Actions

1. Stop the release candidate promotion.
2. Revert to the last known stable release artifacts.
3. Re-run verification and checksum checks.
4. Prevent promotion until the root cause is resolved.
