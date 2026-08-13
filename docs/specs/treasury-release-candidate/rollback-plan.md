# Treasury RC Rollback Plan

1. Stop RC deployment activities.
2. Restore last stable treasury release tag and package.
3. Re-run production readiness and disaster-recovery validation on the restored baseline.
4. Re-open RC gate only after root-cause analysis and remediation are completed.
