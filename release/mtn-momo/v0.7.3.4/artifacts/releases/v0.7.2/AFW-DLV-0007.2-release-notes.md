# AFW-DLV-0007.2 — Release Notes

**Version:** v0.7.2  
**Sprint:** Sprint 7 — Mobile Money, Banking & Card Network

## Nouveautés

### Orange Money Connector

Introduction d'un connecteur sandbox dédié à Orange Money afin de permettre des opérations de paiement mobile de bout en bout dans un environnement contrôlé.

Le connecteur prend désormais en charge :

- dépôt
- retrait
- consultation du statut
- états normalisés
- validation pays, devise, montant et numéro
- idempotence avec hash de requête
- détection des conflits de payload
- référence fournisseur sandbox
- health endpoint interne

## API

Le connecteur expose un modèle de contrat générique ainsi que les flux nécessaires à l'intégration de services de paiement mobile.

## Flutter

Ajout du premier écran de dépôt Orange Money :

**Orange Money Deposit**

Fonctionnalités :

- saisie du montant
- saisie du numéro
- validation locale
- préparation du flux de dépôt
- visualisation de l’état de la demande

## Validation

```text
Backend Build ............ PASS
Scenario Tests ........... PASS
Flutter Analyze .......... PASS
Flutter Tests ............ PASS
```

## Impact

Cette livraison prépare les futures intégrations avec :

- Orange Money
- MTN Mobile Money
- d'autres opérateurs de paiement mobile
