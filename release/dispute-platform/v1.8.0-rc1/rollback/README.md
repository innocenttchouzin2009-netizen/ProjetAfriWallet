# Rollback

See [docs/specs/dispute-release-candidate/rollback-plan.md](../../../../docs/specs/dispute-release-candidate/rollback-plan.md) for the full rollback plan.

In summary: the release tag `dispute-platform-v1.8.0-rc1` is immutable, rollback redeploys the previously approved application artifact, no destructive schema rollback is automatic, and historical dispute records must never be deleted to simulate a rollback.
