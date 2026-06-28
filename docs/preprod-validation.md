# Preproduction Validation Runbook

## Scope

No new features during this phase.
Goal: validate end-to-end business readiness under realistic conditions.

## 1) Functional Validation

### Customer and checkout flows

- [ ] Create at least 3 customer accounts.
- [ ] Place Stripe test-mode orders (minimum: 3).
- [ ] Place PayPal test-mode orders (minimum: 3).
- [ ] Verify confirmation emails for each successful order.
- [ ] Verify order appears in admin orders list.

### Billing and post-order operations

- [ ] Generate and download invoice PDF for test orders.
- [ ] Execute at least 2 refunds and verify payment provider status.
- [ ] Validate order status transitions:
  - [ ] CONFIRMED -> IN_PRODUCTION
  - [ ] IN_PRODUCTION -> READY
  - [ ] READY -> SHIPPED
  - [ ] SHIPPED -> DELIVERED
- [ ] Simulate one in-store sale using POS flow.

### Operational evidence

- [ ] Capture screenshots/log references for each validated scenario.
- [ ] Store evidence in release notes or QA record.

## 2) Load Validation

### Target scenarios

- [ ] Multiple simultaneous checkout requests.
- [ ] Concurrent admin activity (orders filtering/status updates).
- [ ] Baseline response time and error-rate checks.

### Suggested command examples

Run from repository root:

```bash
npx autocannon -c 20 -d 30 -m POST -H "content-type: application/json" -b "{\"customer\":{\"firstName\":\"Load\",\"lastName\":\"Test\",\"email\":\"load@example.com\",\"phone\":\"\"},\"address\":{\"address\":\"1 rue test\",\"postalCode\":\"75001\",\"city\":\"Paris\",\"country\":\"France\"},\"paymentProvider\":\"stripe\",\"items\":[{\"name\":\"Load Cap\",\"quantity\":1,\"unitPrice\":49.9,\"sku\":\"LOAD-001\"}],\"shippingCents\":0,\"discountCents\":0}" http://localhost:3000/api/checkout
```

```bash
npx autocannon -c 10 -d 30 "http://localhost:3000/api/admin/orders?limit=100"
```

### Acceptance baseline (adjust per environment)

- [ ] No sustained 5xx bursts.
- [ ] p95 latency remains acceptable for checkout/admin endpoints.
- [ ] No severe resource saturation on app/database.

## 3) Preproduction Deployment Validation

### Environment

- [ ] Dedicated PostgreSQL preproduction database.
- [ ] Dedicated test domain/subdomain.
- [ ] Stripe/PayPal test credentials configured.
- [ ] Webhooks configured to preproduction endpoints.

### Pipeline and deployment

- [ ] CI pipeline green (`web-ci`).
- [ ] CD preview deployment works (`web-cd`).
- [ ] Migration guard passes (`prisma migrate deploy`).
- [ ] Smoke checks pass on deployed preview URL.

### Final prelaunch decision

- [ ] Functional tests complete.
- [ ] Load tests complete.
- [ ] Preprod deployment validated.
- [ ] Issues triaged/fixed or explicitly accepted.

## Related Docs

- `docs/deployment.md`
- `docs/env.production.md`
- `docs/production-checklist.md`
- `docs/go-live.md`
