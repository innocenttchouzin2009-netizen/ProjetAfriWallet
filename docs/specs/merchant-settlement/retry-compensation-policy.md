# Settlement Retry Compensation Policy

Timeout and temporary provider failure are retryable, up to three attempts. Permanent failure becomes `Failed`; exhaustion becomes `ManualInterventionRequired`; partial failure becomes `CompensationRequired`; successful compensation becomes `Compensated`. Compensation is sandbox orchestration only and claims no financial reversal.
