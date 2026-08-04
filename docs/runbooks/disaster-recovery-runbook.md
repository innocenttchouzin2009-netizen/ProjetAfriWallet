# Disaster Recovery Runbook

## Objectives
- Protect financial ledger data with automated backups and tested restore flows.
- Validate ledger integrity and support point-in-time recovery.
- Rebuild projections and replay outbox events after a recovery event.

## Procedures
1. Trigger a backup through the recovery API.
2. Validate the created backup artifact and retention policy.
3. Restore the backup into the target environment and verify health.
4. Execute PITR with the requested timestamp.
5. Run ledger integrity validation and replay any outbox events.
6. Confirm projections and balances are rebuilt before reopening services.
