# AFW-DLV-0007.1 — Release Notes

**Version:** v0.7.1  
**Sprint:** Sprint 7 — Mobile Money, Banking & Card Network

## Nouveautés

### Mobile Money Registry

Introduction d'un registre unifié permettant de gérer les fournisseurs Mobile Money indépendamment de leur implémentation technique.

Le registre prend désormais en charge :

- identifiant fournisseur
- code unique
- pays
- devise
- environnement (Sandbox / Production)
- statut
- capacités
- limites transactionnelles
- frais
- version d'API

## API

Nouveaux endpoints :

```http
GET    /api/v1/mobile-money/providers
GET    /api/v1/mobile-money/providers/{id}

POST   /internal/mobile-money/providers
PUT    /internal/mobile-money/providers/{id}
DELETE /internal/mobile-money/providers/{id}
```

## Flutter

Ajout du premier écran :

**Mobile Money Registry**

Fonctionnalités :

- recherche
- filtrage
- affichage des fournisseurs
- disponibilité
- statut des services

## Validation

```text
Backend Build ............ PASS
Scenario Tests ........... PASS
Flutter Analyze .......... PASS
Flutter Tests ............ PASS
```

## Impact

Cette livraison prépare les futures intégrations :

- Orange Money
- MTN Mobile Money
- Airtel Money
- Wave
- M-Pesa
- Moov Money
- Free Money
- TMoney
