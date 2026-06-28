# Deployment Guide (RC2)

## Target Stack

- Frontend/API: Next.js app in `apps/web`
- Hosting: Vercel
- Database: PostgreSQL (managed service recommended)

## Vercel Deployment

1. Connect repository to Vercel.
2. Set project root to repository root (monorepo).
3. Configure build settings:
- Build command: `npm run build --workspace web`
- Output directory: `.next` (default for Next.js)
4. Configure environment variables from `docs/env.production.md`.
5. Deploy preview from `main` pull requests.
6. Promote latest green preview to production.

## PostgreSQL Production Setup

1. Provision managed PostgreSQL (Neon/Supabase/RDS/Cloud SQL).
2. Enforce network restrictions:
- Allow only app runtime egress ranges.
- Deny public admin access whenever possible.
3. Create least-privilege database user for app runtime.
4. Set `DATABASE_URL` using SSL-enabled connection string.
5. Apply Prisma migrations before first production traffic:
- `npm run prisma:migrate --workspace web` (or your migration command)

## Release Procedure

1. Merge to `main` only after CI is green.
2. Verify production env vars are set.
3. Run database migration.
4. Deploy application.
5. Smoke test critical routes:
- `/`
- `/checkout`
- `/admin/orders`
- `/api/health`

## Rollback

1. Roll back Vercel deployment to previous stable release.
2. If schema migration is backward-incompatible, restore database from latest verified backup.
3. Re-run smoke tests.

## Notes

- Known non-blocking warning: Sentry ESM import warning (tracked in `docs/production-warnings.md`).
- E2E full run can be environment-dependent; CI runs E2E discovery (`--list`) by default.

## RC2.1 Automation

- CI workflow: `.github/workflows/web-ci.yml`
- CD workflow: `.github/workflows/web-cd.yml`

CD workflow responsibilities:

1. Deploy preview on pull requests.
2. Run production migration guard (`prisma migrate deploy`) before production deployment.
3. Deploy production on push to `main`.
4. Execute post-deploy smoke checks on `/`, `/checkout`, and `/api/health`.
