# Rollback Plan

## Trigger
Rollback if health checks fail, validation report shows failures, or integration tests uncover regressions.

## Actions
1. Stop deployment or promotion.
2. Revert to the previous verified release tag.
3. Re-run validation and confirm stability.
