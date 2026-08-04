# AFW-DLV-0007.3.2 — Release Notes

**Version:** v0.7.3.2  
**Sprint:** Sprint 7 — Mobile Money, Banking & Card Network

## Nouveautés

### MTN MoMo API & Idempotency

Cette livraison étend le connecteur MTN MoMo déjà introduit avec des mécanismes d'intégration plus robustes, en préparation des workflows documentés par MTN dans un environnement sandbox.

Le lot inclut :

- MtnMomoApiClient
- OAuthTokenProvider (simulation sandbox)
- IdempotencyMiddleware
- CorrelationMiddleware
- RequestHashService
- TransactionStatusService
- CallbackVerifier (stub)
- RetryPolicy
- HealthService

## Fonctionnalités

- idempotence complète
- détection de doublons
- hash SHA-256 des requêtes
- Correlation-ID
- retry automatique
- timeout configurable
- journalisation
- suivi des transactions
- simulation d'obtention de jeton OAuth (sans appels réseau)
- préparation du traitement des callbacks

## API AfriWallet

Endpoints exposés :

```http
POST /api/v1/mobile-money/mtn-momo/deposit
POST /api/v1/mobile-money/mtn-momo/withdraw
GET  /api/v1/mobile-money/mtn-momo/status/{reference}
POST /internal/mobile-money/mtn-momo/callback
GET  /internal/mobile-money/mtn-momo/health
```

## Flutter

Ajout de l'écran de suivi de transaction :

- Transaction Status

Fonctionnalités :

- rafraîchissement de l'état
- historique local
- badge de statut
- indicateur de progression

## Documentation

- ADR-0113
- ADR-0114
- ADR-0115
- OpenAPI
- PRD
- Release Notes
- Runbook
- Checklist QA

## Validation

```text
Backend Build ............. PASS
MTN API Scenarios ......... PASS
Idempotency ............... PASS
Retry Policy .............. PASS
Timeout Policy ............ PASS
Flutter Analyze ........... PASS
Flutter Tests ............. PASS
```

## Impact

Cette livraison renforce la fiabilité du connecteur MTN MoMo et prépare l'expérience utilisateur complète de la prochaine étape du Sprint 7.
