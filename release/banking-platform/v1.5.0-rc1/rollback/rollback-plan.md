# Banking Platform Rollback Plan

## Trigger
Any RC validation failure or release gate miss causes rollback to the previous stable release.

## Actions
- stop promotion
- preserve evidence
- keep RC sandbox boundary intact
- retain previous release tag until validation succeeds
