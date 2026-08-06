# Operations Runbook — AFW-DLV-0011.7

## Pre-Release
1. Ensure clean validation environment.
2. Execute validation script in Release mode.
3. Review both JSON and Markdown reports.

## Incident Triage
1. If a functional check fails, run the affected scenario project directly.
2. If health/startup fails, verify appsettings and environment variables.
3. If secret scan fails, inspect findings and sanitize leaked values immediately.

## Recovery
1. Apply rollback plan from `rollback-plan.md`.
2. Re-run the full validation suite before re-attempting release.
