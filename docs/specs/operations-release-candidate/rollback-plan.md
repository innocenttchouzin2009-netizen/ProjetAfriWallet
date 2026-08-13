# Rollback Plan

1. Stop rollout.
2. Revert release commit if required.
3. Restore previous validated tag.
4. Re-run validation gate before next promotion.
