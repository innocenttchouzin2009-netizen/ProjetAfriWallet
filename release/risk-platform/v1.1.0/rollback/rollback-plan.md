# Rollback Plan — AFW-DLV-0011.7

## Trigger Conditions
- Any failed critical check in production validation.
- Secret leak detection in release candidate artifacts.
- Runtime instability after deployment verification.

## Steps
1. Stop release promotion and freeze rollout.
2. Revert to previous stable main commit/tag.
3. Redeploy prior release artifacts.
4. Verify health and readiness endpoints.
5. Re-run impacted scenario suites.
6. Produce post-incident report and corrective actions.
