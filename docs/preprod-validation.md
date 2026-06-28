# Preproduction Validation Runbook

No new features during this phase.
Goal: validate end-to-end business readiness under realistic conditions.

## Phase 1 - Go Preview

### Infrastructure and secrets

- [ ] `DATABASE_URL`
- [ ] `DIRECT_URL` (if used by Prisma)
- [ ] `NEXTAUTH_SECRET`
- [ ] `NEXTAUTH_URL`
- [ ] `STRIPE_SECRET_KEY`
- [ ] `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY`
- [ ] `STRIPE_WEBHOOK_SECRET`
- [ ] `PAYPAL_CLIENT_ID`
- [ ] `PAYPAL_CLIENT_SECRET`
- [ ] `PAYPAL_WEBHOOK_ID`
- [ ] Email provider API key
- [ ] Sentry variables (if enabled)

### Pipeline gates (no skip)

- [ ] Installation
- [ ] Lint
- [ ] Typecheck
- [ ] Unit tests
- [ ] E2E tests
- [ ] Build
- [ ] `prisma migrate deploy`
- [ ] Smoke tests

### Preproduction deployment

- [ ] Dedicated PostgreSQL preproduction database
- [ ] Dedicated test domain/subdomain
- [ ] Stripe and PayPal sandbox setup
- [ ] Sandbox webhooks configured
- [ ] `web-ci` green
- [ ] `web-cd` preview green

## Phase 2 - Business Validation (20+ scenarios)

### Authentication

- [ ] Account creation
- [ ] Login
- [ ] Logout
- [ ] Forgot password
- [ ] Password reset
- [ ] Role checks

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
- [ ] Status change
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
- [ ] Out-of-stock rejection
- [ ] Manual adjustment

### Load and performance

- [ ] Concurrent checkout requests
- [ ] Concurrent admin sessions
- [ ] p95 and error-rate baseline accepted

Suggested local load checks:

```bash
npx autocannon -c 20 -d 30 -m POST -H "content-type: application/json" -b "{\"customer\":{\"firstName\":\"Load\",\"lastName\":\"Test\",\"email\":\"load@example.com\",\"phone\":\"\"},\"address\":{\"address\":\"1 rue test\",\"postalCode\":\"75001\",\"city\":\"Paris\",\"country\":\"France\"},\"paymentProvider\":\"stripe\",\"items\":[{\"name\":\"Load Cap\",\"quantity\":1,\"unitPrice\":49.9,\"sku\":\"LOAD-001\"}],\"shippingCents\":0,\"discountCents\":0}" http://localhost:3000/api/checkout
npx autocannon -c 10 -d 30 "http://localhost:3000/api/admin/orders?limit=100"
```

## Phase 3 - Go Live

Proceed only when all are true:

- [ ] 100% critical scenarios validated
- [ ] No blocking bug
- [ ] No E2E regression

Release commands:

```bash
git tag v1.0.0
git push origin v1.0.0
```

## Related Docs

- `docs/deployment.md`
- `docs/env.production.md`
- `docs/production-checklist.md`
- `docs/go-live.md`
