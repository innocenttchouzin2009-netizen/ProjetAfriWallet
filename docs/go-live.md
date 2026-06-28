# Go-Live Runbook (RC2.2)

## 1. Pre-Flight

1. Complete all checks in `docs/production-checklist.md`.
2. Complete preproduction validation runbook in `docs/preprod-validation.md`.
3. Confirm latest `main` commit is green in CI (`web-ci` + deployment workflow readiness).
4. Confirm Vercel production environment variables are present and up to date.

## 2. Database Preparation

1. Verify production database health and connectivity.
2. Run migration guard:

```bash
npm run prisma:migrate:deploy --workspace web
npm run prisma:migrate:status --workspace web
```

3. Confirm no unexpected migration drift.

## 3. Deploy

1. Trigger production deployment (push to `main` or manual workflow dispatch).
2. Wait for deployment completion and capture deployment URL/build ID.
3. Ensure smoke checks pass:
   - `/`
   - `/checkout`
   - `/api/health` (200 or 503 during transient DB maintenance windows)

## 4. Payment/Operations Validation

1. Execute one real or controlled low-value complete order.
2. Validate payment capture in Stripe/PayPal live dashboards.
3. Validate webhook reception and processing logs.
4. Execute one refund and validate status reconciliation.
5. Validate shipping creation flow.
6. Validate invoice PDF generation/download.

## 5. Post-Go-Live Monitoring

1. Watch error rate and latency dashboards for 30-60 minutes.
2. Watch payment failures and webhook failures.
3. Confirm audit logs are generated for key operations.

## 6. Rollback Criteria and Action

Rollback if any of the following occurs:
- sustained checkout/payment failures,
- migration issue affecting read/write operations,
- critical regression on order lifecycle.

Rollback actions:
1. Revert Vercel deployment to last known good release.
2. If required, restore PostgreSQL from validated backup point.
3. Re-run smoke tests and communicate status.

## 7. Release Record

Record the following in release notes:
- deployment timestamp,
- git tag and commit hash,
- migration version applied,
- validation evidence for order/refund/shipping/invoice,
- backup verification evidence.
