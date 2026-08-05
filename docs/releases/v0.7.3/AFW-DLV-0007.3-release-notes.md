# AFW-DLV-0007.3 — Release Notes

**Version:** v0.7.3  
**Sprint:** Sprint 7 — Mobile Money, Banking & Card Network

## Nouveautés

### MTN MoMo Sandbox Connector

Introduction d'un connecteur sandbox dédié à MTN MoMo afin de permettre des opérations de dépôt et de retrait dans un environnement contrôlé et compatible avec la logique de connecteurs Mobile Money déjà mise en place.

Le connecteur prend désormais en charge :

- dépôt
- retrait
- consultation du statut de transaction
- idempotence
- request hash
- correlation ID
- health endpoint
- disponibilité du fournisseur
- validation des entrées
- scénario runner

## API

Nouveaux endpoints :

```http
POST /api/v1/mobile-money/mtn-momo/deposit
POST /api/v1/mobile-money/mtn-momo/withdraw
GET  /api/v1/mobile-money/mtn-momo/transactions/{reference}
GET  /internal/mobile-money/mtn-momo/health
```

## Flutter

Ajout des écrans de dépôt et retrait MTN Mobile Money :

- MTN Mobile Money Deposit
- MTN Mobile Money Withdraw

Fonctionnalités :

- saisie du montant
- saisie du numéro
- validation locale
- préparation du flux de dépôt/retrait
- consultation de l'état de la transaction

## Documentation

- ADR
- OpenAPI
- Release Notes
- PRD
- Runbook

## Validation

```text
Backend Build ............ PASS
Scenario Tests ........... PASS
Flutter Analyze .......... PASS
Flutter Tests ............ PASS
```

## Impact

Cette livraison prépare les futures intégrations avec le portefeuille Mobile Money MTN, tout en conservant un modèle sandbox et modulable.
