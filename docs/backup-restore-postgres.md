# PostgreSQL Backup and Restore Runbook

## Objective

Provide automated backups and a documented restore strategy before production go-live.

## Automated backup (recommended daily + retention)

Example using pg_dump in a scheduled task/cron:

```bash
pg_dump "$DATABASE_URL" -Fc -f "backup_$(date +%Y%m%d_%H%M%S).dump"
```

Recommendations:

- Frequency: daily full backup + optional intra-day snapshot for peak periods.
- Retention: 14 to 30 days depending on legal/operational constraints.
- Storage: copy encrypted backups to offsite object storage (S3-compatible).
- Verification: run weekly restore test on staging.

## Restore procedure

1. Identify target backup file and maintenance window.
2. Stop writes on application side.
3. Restore into target database:

```bash
pg_restore --clean --if-exists --no-owner --no-privileges -d "$TARGET_DATABASE_URL" backup_YYYYMMDD_HHMMSS.dump
```

4. Run app health check:

- /api/health should return status ok.

5. Re-enable traffic/writes.
6. Record incident timeline and checksum of restored backup.

## Minimal validation checklist after restore

- User login works.
- Checkout route returns success for valid payload.
- Admin orders list loads.
- Invoice download endpoint responds.
- Inventory overview endpoint responds.

## Security

- Never store backups unencrypted.
- Restrict backup bucket access with least privilege.
- Rotate database credentials and backup access keys periodically.
