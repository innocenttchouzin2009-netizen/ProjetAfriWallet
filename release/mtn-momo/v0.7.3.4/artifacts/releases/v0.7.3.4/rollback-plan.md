# Rollback Plan

1. Stop or disable the release candidate deployment.
2. Revert to the previous known-good deployment artifact.
3. Restore the previous configuration and feature-flag state.
4. Re-run the validation gate to confirm the rollback state.
