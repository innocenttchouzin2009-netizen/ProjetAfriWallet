# Production Final Checklist (RC2.2)

## CI/CD and Secrets

- [ ] GitHub Actions secrets configured:
  - [ ] `VERCEL_TOKEN`
  - [ ] `VERCEL_ORG_ID`
  - [ ] `VERCEL_PROJECT_ID`
  - [ ] `DATABASE_URL`
  - [ ] `DIRECT_URL` (if used by Prisma)
- [ ] Authentication secrets configured:
  - [ ] `NEXTAUTH_SECRET`
  - [ ] `NEXTAUTH_URL`
- [ ] Vercel environment variables configured for Production:
  - [ ] `NODE_ENV=production`
  - [ ] `NEXT_PUBLIC_APP_URL`
  - [ ] `DATABASE_URL`
  - [ ] `DIRECT_URL` (if used by Prisma)
  - [ ] `STRIPE_SECRET_KEY`
  - [ ] `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY`
  - [ ] `STRIPE_WEBHOOK_SECRET`
  - [ ] `PAYPAL_CLIENT_ID`
  - [ ] `PAYPAL_CLIENT_SECRET`
  - [ ] `PAYPAL_WEBHOOK_ID`
  - [ ] Email provider API key
  - [ ] Sentry variables (if enabled)

## Pipeline Gates (No Skips)

- [ ] Install dependencies passes.
- [ ] Lint passes.
- [ ] Typecheck passes.
- [ ] Unit tests pass.
- [ ] E2E tests pass.
- [ ] Build passes.
- [ ] `prisma migrate deploy` passes.
- [ ] Smoke tests pass.

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

- [ ] 100% critical scenarios validated.
- [ ] No blocking bug remains.
- [ ] No E2E regression remains.

## Business Scenarios (Minimum Set)

### Authentication

- [ ] Account creation
- [ ] Login
- [ ] Logout
- [ ] Forgot password
- [ ] Reset password
- [ ] Role access control

### Catalog

- [ ] Search
- [ ] Filters
- [ ] Add to cart
- [ ] Studio Designer
- [ ] Save design

### Payment

- [ ] Stripe success
- [ ] Stripe cancellation
- [ ] PayPal success
- [ ] PayPal cancellation
- [ ] Webhook processing

### Orders

- [ ] Order creation
- [ ] Status updates
- [ ] Production flow
- [ ] Shipping flow
- [ ] Delivery flow

### Documents

- [ ] Invoice PDF
- [ ] Delivery note PDF

### Refunds

- [ ] Full refund
- [ ] Partial refund
- [ ] Duplicate refund rejected

### POS

- [ ] Basic sale
- [ ] Sale with discount
- [ ] Receipt print/export

### Inventory

- [ ] Stock decrement
- [ ] Out-of-stock guard
- [ ] Manual stock adjustment

## Backup and Recovery

- [ ] PostgreSQL backup policy enabled and verified.
- [ ] Last backup success confirmed.
- [ ] Restore drill validated (or latest successful restore evidence recorded).

## Final Sign-off

- [ ] Technical owner approval
- [ ] Product/operations approval
- [ ] Go-live window confirmed
- [ ] Incident rollback owner assigned
