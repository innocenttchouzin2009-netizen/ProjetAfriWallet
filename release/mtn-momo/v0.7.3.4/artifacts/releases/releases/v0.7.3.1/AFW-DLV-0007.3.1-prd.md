# PRD — AFW-DLV-0007.3.1

**Titre :** MTN MoMo Connector Core

**Objectif :**
Implémenter la première couche fonctionnelle du connecteur MTN MoMo sandbox afin de permettre des opérations de dépôt et retrait dans un environnement contrôlé.

## Contexte métier

AfriWallet doit pouvoir intégrer progressivement des opérateurs de paiement mobile, tout en conservant une architecture modulaire, extensible et indépendante des fournisseurs spécifiques.

## User Story

> En tant qu'utilisateur AfriWallet,
> 
> je souhaite effectuer un dépôt ou un retrait via MTN MoMo depuis l'application,
> 
> afin de transférer des fonds vers mon wallet ou vers un opérateur mobile de manière sécurisée et tracée.

## Fonctionnalités attendues

Le système doit permettre :

- d'exécuter un dépôt via un connecteur MTN MoMo sandbox ;
- d'exécuter un retrait via un connecteur MTN MoMo sandbox ;
- de consulter l'état d'une transaction ;
- de valider les entrées utilisateur ;
- d'assurer l'idempotence des requêtes ;
- de calculer et transmettre un request hash ;
- de détecter les conflits de payload ;
- d'exposer un health endpoint interne.

## Critères d'acceptation

- Un connecteur MTN MoMo sandbox est implémenté.
- Les opérations de dépôt et retrait sont prises en charge dans un environnement sandbox.
- Les transactions sont traçables et validées de manière structurée.
- Les requêtes sont protégées contre les doublons et conflits.
- Le flux est visible via des écrans Flutter dédiés.
- La documentation technique et la validation fonctionnelle sont fournies.

## Hors périmètre

Cette livraison n'inclut pas :

- les vrais appels API MTN MoMo ;
- les secrets ou identifiants d'application réels ;
- l'authentification OAuth réelle ;
- l'exécution de transactions réelles.

## Décision d'architecture

Le connecteur MTN MoMo doit être conçu comme une implémentation sandbox de la couche générique de connecteurs Mobile Money afin de garantir :

- une extensibilité future ;
- une compatibilité avec les autres opérateurs ;
- une séparation claire entre logique métier et intégration fournisseur.
