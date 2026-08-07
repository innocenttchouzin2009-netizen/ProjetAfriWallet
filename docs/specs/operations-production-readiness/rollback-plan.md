# Rollback Plan

1. Halt release promotion.
2. Revert offending commit on main.
3. Re-run readiness checks and scenarios.
4. Regenerate reports and checksums.
5. Reopen readiness PR if needed.
