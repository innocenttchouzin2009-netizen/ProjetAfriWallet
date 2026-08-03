# Go-Live Checklist (Web)

## 1) Variables d'environnement (bloquant)
Configurer toutes les variables sur l'hebergeur (Production), sur la base de [apps/web/.env.production.example](apps/web/.env.production.example):

- DATABASE_URL
- AUTH_SECRET
- NEXT_PUBLIC_APP_URL
- NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY
- STRIPE_SECRET_KEY
- STRIPE_WEBHOOK_SECRET
- PAYPAL_MODE=live
- PAYPAL_CLIENT_ID
- PAYPAL_CLIENT_SECRET
- PAYPAL_WEBHOOK_ID

Recommandees:
- CLOUDINARY_CLOUD_NAME
- CLOUDINARY_API_KEY
- CLOUDINARY_API_SECRET
- SENTRY_DSN
- SENTRY_ENVIRONMENT=production
- SENTRY_TRACES_SAMPLE_RATE=0.2
- ADMIN_ALERT_WEBHOOK_URL
- RC_ENFORCE_RBAC=true

## 2) Build / qualite (bloquant)
Depuis la racine du workspace:

```powershell
npm --prefix "apps/web" run lint
npm --prefix "apps/web" run typecheck
npm --prefix "apps/web" run test
npm --prefix "apps/web" run build
```

Attendu:
- Lint: 0 error (warnings acceptables temporairement)
- Typecheck: OK
- Tests: OK
- Build: OK

## 3) Base de donnees (bloquant)
Apres configuration de DATABASE_URL en production:

```powershell
npm --prefix "apps/web" run prisma:migrate:deploy
```

Optionnel (controle):

```powershell
npm --prefix "apps/web" run prisma:migrate:status
```

## 4) Endpoints sante et smoke tests (bloquant)
Verifier l'API apres deploiement:

- GET /api/health doit retourner 200 avec `status: ok` et `db: up`.
- Le login admin doit repondre 200 via /api/auth/login.
- La page checkout doit pouvoir creer une session via /api/payments/checkout.

Exemple:

```powershell
curl https://<ton-domaine>/api/health
```

## 5) Webhooks paiements (bloquant)
Configurer dans Stripe et PayPal:

- Stripe webhook URL: https://<ton-domaine>/api/payments/webhook/stripe
- PayPal webhook URL: https://<ton-domaine>/api/payments/webhook/paypal

Verifier que les webhooks retournent `{ ok: true }` en cas de signature/verification valide.

## 6) Verifications metier post-deploiement (fortement recommande)
- Passer une commande test Stripe (petit montant) jusqu'a confirmation.
- Passer une commande test PayPal (sandbox puis live si possible).
- Verifier creation de commande + statut dans l'admin.
- Verifier un remboursement depuis l'admin.
- Verifier upload image produit via Cloudinary.

## 7) Notes de securite et robustesse
- L'application bloque maintenant le demarrage en production sans DATABASE_URL explicite.
- Les cookies auth sont `secure` en production.
- Activer RC_ENFORCE_RBAC=true en production.

## 8) E2E (optionnel avant go-live strict)
```powershell
npm --prefix "apps/web" run test:e2e
```

Notes:
- Les specs E2E utilisent des mocks pour certains flux.
- Pour les scenarios facture/remboursement, definir `E2E_ORDER_ID`.
