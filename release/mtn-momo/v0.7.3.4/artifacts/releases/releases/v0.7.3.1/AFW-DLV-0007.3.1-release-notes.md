# AFW-DLV-0007.3.1 — Release Notes

**Version:** v0.7.3.1  
**Sprint:** Sprint 7 — Mobile Money, Banking & Card Network

## Nouveautés

### MTN MoMo Connector Core

Introduction d'un connecteur sandbox dédié à MTN MoMo, visant à fournir la base fonctionnelle nécessaire aux opérations de dépôt et retrait dans un environnement contrôlé.

Le lot inclut :

- MtnMomoSandboxConnector
- dépôt MTN MoMo
- retrait MTN MoMo
- consultation du statut
- validation du pays, de la devise, du montant et du numéro
- références fournisseur sandbox
- idempotence
- hash de requête
- détection de conflit
- health endpoint
- scénario runner

## API

Le connecteur expose les flux de base nécessaires à l'intégration de l'expérience MTN MoMo au sein d'AfriWallet.

## Flutter

Ajout des écrans de dépôt et retrait MTN Mobile Money :

- MTN Mobile Money Deposit
- MTN Mobile Money Withdraw

Fonctionnalités :

- saisie du montant
- saisie du numéro
- validation locale
- préparation des flux de dépôt/retrait
- consultation de l'état de la transaction

## Documentation

- ADR
- OpenAPI
- PRD
- Release Notes
- Runbook

## Validation

```text
Backend Build ............ PASS
Scenario Tests ........... PASS
Flutter Analyze .......... PASS
Flutter Tests ............ PASS
```

## Impact

Cette livraison pose les fondations du connecteur MTN MoMo en environnement sandbox et prépare les futures intégrations réelles.
