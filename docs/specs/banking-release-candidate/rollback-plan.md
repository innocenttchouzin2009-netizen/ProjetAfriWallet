# Banking Platform Rollback Plan

## Goal
Return the system to the last known stable release if the RC verification fails.

## Actions
- stop release promotion
- preserve immutable evidence
- keep sandbox-only enforcement in place
- revert the RC tag until validation is complete
