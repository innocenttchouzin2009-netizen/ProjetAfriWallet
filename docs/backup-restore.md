# Backup and Restore Strategy (RC2)

## Objectives

- Protect order, payment, inventory, and audit data.
- Guarantee point-in-time recovery for operational incidents.

## Backup Policy

- Full database backup: daily.
- Incremental/WAL backup: every 5-15 minutes (managed service dependent).
- Retention:
- Daily backups: 14 days.
- Weekly backups: 8 weeks.
- Monthly backups: 12 months.

## Storage and Security

- Store backups in encrypted object storage.
- Enable immutability/retention lock for ransomware resilience.
- Restrict access to SRE/ops roles only.

## Restore Runbook

1. Identify restore target timestamp.
2. Create isolated restore instance from latest backup + WAL replay.
3. Validate data integrity checks:
- latest orders
- inventory counts
- audit log continuity
4. Switch app `DATABASE_URL` to restored instance during maintenance window.
5. Run application smoke tests and E2E sanity check.

## Verification Cadence

- Monthly restore drill in staging.
- Track RPO/RTO metrics after each drill.

## Operational Checklist

- [ ] Backups enabled and monitored
- [ ] Alerting on backup failures
- [ ] Quarterly access review for backup storage
- [ ] Documented ownership and on-call escalation path

## Related Docs

- `docs/deployment.md`
- `docs/env.production.md`
- `docs/backup-restore-postgres.md`
