# PRD — AFW-DLV-0007.2

**Titre :** Orange Money Connector

**Objectif :**
Permettre à AfriWallet d'interagir avec un connecteur Orange Money sandbox afin de préparer les futurs flux réels de dépôt et retrait.

## Contexte métier

AfriWallet doit pouvoir intégrer progressivement des opérateurs de paiement mobile de manière contrôlée et modulaire, sans dépendre d'implémentations techniques spécifiques au fournisseur.

## User Story

> En tant qu'utilisateur AfriWallet,
> 
> je souhaite effectuer un dépôt Orange Money depuis l'application,
> 
> afin de transférer des fonds vers mon wallet de manière sécurisée et tracée.

## Fonctionnalités attendues

Le système doit permettre :

- d'exécuter un dépôt via un connecteur Orange Money sandbox ;
- d'exécuter un retrait via un connecteur Orange Money sandbox ;
- de consulter l'état d'une opération ;
- de valider pays, devise, montant et numéro ;
- d'assurer l'idempotence des requêtes ;
- de détecter les conflits de payload ;
- d'exposer un health endpoint interne.

## Critères d'acceptation

- Un connecteur générique est défini pour les opérateurs Mobile Money.
- Un connecteur Orange Money sandbox est implémenté.
- Les opérations de dépôt et retrait sont prises en charge dans un environnement sandbox.
- Les états sont normalisés et exposés de manière cohérente.
- Les requêtes sont protégées contre les doublons et conflits.
- Le flux est visible via un écran Flutter dédié.

## Hors périmètre

Cette livraison n'inclut pas :

- les vrais appels API Orange Money ;
- la gestion d'authentification OAuth ou de secrets réels ;
- l'exécution de transactions réelles ;
- la production de données financières officielles.

## Décision d'architecture

Le connecteur Orange Money doit être conçu comme une implémentation sandbox de la couche générique de connecteurs Mobile Money afin de garantir :

- une extensibilité future ;
- une compatibilité avec les autres opérateurs ;
- une séparation claire entre logique métier et intégration fournisseur.
