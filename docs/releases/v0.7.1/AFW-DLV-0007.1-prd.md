# PRD — AFW-DLV-0007.1

**Titre :** Mobile Money Registry

**Objectif :**
Créer une couche d'abstraction unique permettant à AfriWallet d'utiliser plusieurs opérateurs Mobile Money sans modifier le cœur métier.

## Contexte métier

AfriWallet doit pouvoir intégrer progressivement des opérateurs de paiement mobile variés, tout en conservant une architecture modulaire, extensible et indépendante des fournisseurs spécifiques.

## User Story

> En tant qu'utilisateur AfriWallet,
> 
> je souhaite sélectionner mon opérateur Mobile Money,
> 
> afin d'effectuer des dépôts, retraits et paiements.

## Fonctionnalités attendues

Le système doit permettre :

- d'ajouter un opérateur ;
- de modifier un opérateur ;
- de désactiver un opérateur ;
- de rechercher un opérateur ;
- de filtrer par pays ;
- de filtrer par devise ;
- de filtrer par capacité ;
- de distinguer Sandbox et Production ;
- de gérer les limites transactionnelles ;
- de gérer les frais.

## Critères d'acceptation

- La plateforme expose un registre centralisé des opérateurs Mobile Money.
- Les opérateurs peuvent être créés, modifiés et désactivés via une interface interne.
- Les utilisateurs peuvent consulter les opérateurs via une API publique.
- Le registre supporte les filtres par pays, devise, capacité, statut et environnement.
- Les opérateurs sont initialement configurés en mode Sandbox / Coming Soon.
- Les règles de frais et limites sont représentées de manière structurée.

## Hors périmètre

Cette livraison n'inclut pas :

- la connexion Orange Money ;
- la connexion MTN Mobile Money ;
- les appels API réels ;
- l'authentification OAuth des opérateurs ;
- l'exécution de transactions.

Ces éléments seront traités dans les livraisons suivantes du Sprint 7.

## Décision d'architecture

Le registre Mobile Money doit être conçu comme une couche indépendante des connecteurs techniques réels afin de garantir :

- extensibilité,
- maintenabilité,
- préparation aux intégrations bancaires et de cartes.
