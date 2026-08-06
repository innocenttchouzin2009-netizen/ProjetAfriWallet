# Rollback Plan

1. Stop the RC deployment.
2. Restore the last known-good merchant build.
3. Re-activate the prior configuration and verify health endpoints.
4. Review audit trails and observability signals before re-attempting rollout.
