# Production Environment Variables (RC2)

## Required

- `NODE_ENV=production`
- `NEXT_PUBLIC_APP_URL` (public HTTPS URL, for example `https://your-domain.com`)
- `DATABASE_URL` (PostgreSQL connection string with SSL)

## Authentication

- `BCRYPT_SALT_ROUNDS` (optional, defaults acceptable if not set)

## Payments

- `STRIPE_SECRET_KEY`
- `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY`
- `STRIPE_WEBHOOK_SECRET`
- `PAYPAL_CLIENT_ID`
- `PAYPAL_CLIENT_SECRET`
- `PAYPAL_ENV` (`sandbox` or `live`)

## Monitoring

- `SENTRY_DSN` (optional but recommended)
- `SENTRY_ENVIRONMENT` (for example `production`)
- `SENTRY_TRACES_SAMPLE_RATE` (for example `0.2`)

## Email/Notifications

- Provider-specific SMTP/API variables used by notification services

## Security

- Any secret keys/tokens used by integrations should be set in Vercel Project Settings and never committed

## CI/CD Secrets (GitHub Actions)

- `VERCEL_TOKEN`
- `VERCEL_ORG_ID`
- `VERCEL_PROJECT_ID`
- `DATABASE_URL`

## Example Template

```env
NODE_ENV=production
NEXT_PUBLIC_APP_URL=https://example.com
DATABASE_URL=postgresql://user:password@host:5432/dbname?sslmode=require

STRIPE_SECRET_KEY=sk_live_xxx
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_live_xxx
STRIPE_WEBHOOK_SECRET=whsec_xxx

PAYPAL_CLIENT_ID=xxx
PAYPAL_CLIENT_SECRET=xxx
PAYPAL_ENV=live

SENTRY_DSN=https://examplePublicKey@o0.ingest.sentry.io/0
SENTRY_ENVIRONMENT=production
SENTRY_TRACES_SAMPLE_RATE=0.2
```
