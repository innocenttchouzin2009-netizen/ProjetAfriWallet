# Production Warnings

## DATABASE_URL During Build

Observed warning during `next build`:
- Prisma connection warning can appear if a route with database access is evaluated at build-time.

Applied mitigation:
- Sensitive handlers are now forced dynamic with `export const dynamic = 'force-dynamic';`.
- This was applied to `apps/web/src/app/api/health/route.ts` and all `apps/web/src/app/api/admin/**/route.ts` handlers.

Operational note:
- In CI/production, keep `DATABASE_URL` configured for runtime routes.
- The warning should no longer be triggered by static prerender of these sensitive routes.

## Sentry ESM Warning

Observed warning during `next build`:
- `@sentry/server-utils` references ESM modules from a CJS path and Next.js reports a warning.

Status:
- Non-blocking for current build (build completes successfully).

Decision for RC1:
- Keep current Sentry setup as-is for now.
- Track and revisit when upgrading Sentry/Next.js or when adopting a fully ESM-compatible server instrumentation path.

Current Sentry setup location:
- `apps/web/src/lib/monitoring/sentry.ts`
