# RC1 Quality Plan (Implemented Baseline)

## 1) End-to-End tests with Playwright

Implemented:

- Playwright installed in workspace web.
- Config created: apps/web/playwright.config.ts
- Critical scenarios created: apps/web/e2e/critical-flow.spec.ts

Covered scenarios:

- register;
- login;
- studio personalization;
- add to cart;
- checkout API/sandbox validation;
- invoice download (env-driven order id);
- refund API (env-driven order id).

Run:

- npm run test:e2e --workspace web

Environment note:

- Set E2E_ORDER_ID to execute invoice/refund assertions on an existing paid order.

## 2) Performance

Implemented baseline:

- Next image optimization (AVIF/WebP, remote patterns).
- next/image on product cards (lazy image loading).
- Lazy loading for heavy shop/studio components.

Targets to monitor:

- initial load < 2s;
- Lighthouse score > 95.

Suggested command:

- npm run perf:lighthouse --workspace web

## 3) Security

Implemented baseline:

- Middleware CSP + security headers.
- Origin-based CSRF protection for mutating API requests.
- In-memory rate limiting by IP/path with stricter auth/admin thresholds.
- Zod validation applied to high-risk routes (register, checkout, admin refund, admin inventory adjust).
- RBAC helper and role checks on key admin routes.

RBAC activation:

- Set RC_ENFORCE_RBAC=true in environment to strictly enforce role header checks.

## 4) Observability

Implemented:

- Structured JSON logger utility.
- Sentry integration helper (enabled when SENTRY_DSN is present).
- Health endpoint: /api/health.
- Admin health dashboard: /admin/health.

## 5) Backup and Restore

See backup runbook:

- docs/backup-restore-postgres.md
