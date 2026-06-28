# Production Final Checklist (RC2.2)

## CI/CD and Secrets

- [ ] GitHub Actions secrets configured:
  - [ ] `VERCEL_TOKEN`
  - [ ] `VERCEL_ORG_ID`
  - [ ] `VERCEL_PROJECT_ID`
  - [ ] `DATABASE_URL`
- [ ] Vercel environment variables configured for Production:
  - [ ] `NODE_ENV=production`
  - [ ] `NEXT_PUBLIC_APP_URL`
  - [ ] `DATABASE_URL`
  - [ ] Stripe live keys/secrets
  - [ ] PayPal live credentials
  - [ ] Monitoring and notification variables

## Database and Migrations

- [ ] PostgreSQL production instance is ready (networking, SSL, restricted access).
- [ ] Application DB user has least-privilege access.
- [ ] `prisma migrate deploy` validated on production target.
- [ ] Schema status checked (`prisma migrate status`) with no pending unexpected state.

## Payments and Webhooks

- [ ] Stripe is configured in live mode.
- [ ] PayPal is configured in live mode.
- [ ] Stripe live webhook endpoint configured and tested.
- [ ] PayPal live webhook endpoint configured and tested.

## Platform and Domain

- [ ] Production domain connected to Vercel project.
- [ ] HTTPS certificate is active and valid.
- [ ] DNS propagation confirmed.

## End-to-End Business Validation

- [ ] Complete order flow tested successfully in production-like environment.
- [ ] Refund flow tested successfully.
- [ ] Shipping flow tested successfully.
- [ ] Invoice PDF generation and download tested successfully.

## Backup and Recovery

- [ ] PostgreSQL backup policy enabled and verified.
- [ ] Last backup success confirmed.
- [ ] Restore drill validated (or latest successful restore evidence recorded).

## Final Sign-off

- [ ] Technical owner approval
- [ ] Product/operations approval
- [ ] Go-live window confirmed
- [ ] Incident rollback owner assigned
