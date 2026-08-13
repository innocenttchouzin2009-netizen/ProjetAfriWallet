# Banking Platform Rollback Plan

## Triggers
Rollback may be initiated if:
- readiness CI fails
- provider integration becomes unhealthy
- security validation fails
- dependency policy fails
- reconciliation integrity is compromised
- unexpected production configuration is detected

## Principle
Rollback MUST NOT rewrite historical banking transactions.
Rollback affects deployable application state only.
Historical intents, routing decisions, executions, settlements, reconciliations and audit evidence remain immutable.

## Recovery
1. Disable provider integrations.
2. Stop new bank-transfer execution.
3. Preserve current audit and reconciliation evidence.
4. Restore last verified deployment.
5. Re-run Banking Platform readiness validation.
6. Resume only after approval.
