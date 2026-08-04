# PRD — AFW-DLV-0007.3

**Titre :** MTN MoMo Sandbox Connector

**Objectif :**
Permettre à AfriWallet d'interagir avec un connecteur MTN MoMo sandbox, en respectant la philosophie d'abstraction des connecteurs Mobile Money déjà définie.

## Contexte métier

AfriWallet doit pouvoir intégrer progressivement des opérateurs de paiement mobile de manière contrôlée, modulaire et indépendante des implémentations spécifiques au fournisseur.

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
- d'assurer l'idempotence des requêtes ;
- de calculer et transmettre un request hash ;
- de suivre une correlation ID ;
- de vérifier la disponibilité du fournisseur ;
- de valider les entrées utilisateur ;
- d'exposer un health endpoint interne.

## Critères d'acceptation

- Un connecteur générique est défini pour les opérateurs Mobile Money.
- Un connecteur MTN MoMo sandbox est implémenté.
- Les opérations de dépôt et retrait sont prises en charge dans un environnement sandbox.
- Les transactions sont traçables grâce à un identifiant de corrélation.
- Les requêtes sont protégées contre les doublons et conflits.
- Le flux est visible via des écrans Flutter dédiés.
- La documentation technique et la validation fonctionnelle sont fournies.

## Hors périmètre

Cette livraison n'inclut pas :

- les vrais appels API MTN MoMo ;
- les secrets ou identifiants d'application réels ;
- l'authentification OAuth réelle ;
- l'exécution de transactions réelles ;
- l'intégration avec des services tiers non sandbox.

## Décision d'architecture

Le connecteur MTN MoMo doit être conçu comme une implémentation sandbox de la couche générique de connecteurs Mobile Money afin de garantir :

- une extensibilité future ;
- une compatibilité avec les autres opérateurs ;
- une séparation claire entre logique métier et intégration fournisseur.

## Proposition de découpage recommandé

Pour garder des livraisons atomiques et faciles à tester, le lot peut être découpé en :

- AFW-DLV-0007.3.1 — MTN MoMo Connector Core
- AFW-DLV-0007.3.2 — API & Idempotency
- AFW-DLV-0007.3.3 — Flutter Integration
- AFW-DLV-0007.3.4 — Production Readiness
