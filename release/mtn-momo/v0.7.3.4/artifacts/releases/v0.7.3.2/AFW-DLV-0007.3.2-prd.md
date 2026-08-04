# PRD — AFW-DLV-0007.3.2

**Titre :** MTN MoMo API & Idempotency

**Objectif :**
Renforcer la couche d'intégration MTN MoMo avec des mécanismes d'idempotence, de traçabilité et de sécurité applicative, tout en restant strictement en environnement sandbox.

## Contexte métier

AfriWallet doit pouvoir intégrer des opérateurs de paiement mobile de manière robuste, avec des garanties de sécurité, de traçabilité et de répétabilité des appels, avant toute intégration réelle avec un fournisseur partenaire.

## User Story

> En tant qu'utilisateur AfriWallet,
> 
> je souhaite que mes transactions MTN MoMo soient traitées de manière fiable et traçable,
> 
> afin d'éviter les doublons, les erreurs de synchronisation et les pertes de suivi.

## Fonctionnalités attendues

Le système doit permettre :

- d'assurer l'idempotence complète des opérations ;
- de détecter les doublons de requête ;
- de produire un hash SHA-256 des requêtes ;
- d'appliquer un Correlation-ID à chaque transaction ;
- de mettre en place une politique de retry automatique ;
- de configurer des timeouts applicatifs ;
- de journaliser les traitements ;
- de suivre l'état des transactions ;
- de simuler l'obtention d'un jeton OAuth en sandbox ;
- de préparer le traitement des callbacks.

## Critères d'acceptation

- Les endpoints AfriWallet exposent des flux de dépôt, retrait et suivi de transaction.
- Les requêtes sont protégées contre les doublons via un mécanisme d'idempotence.
- Chaque transaction reçoit un identifiant de corrélation.
- Les retries et timeouts sont configurables et testés.
- Les traitements sont suivis et journalisés de manière cohérente.
- Le mode sandbox reste strictement isolé des endpoints et secrets réels.

## Hors périmètre

Cette livraison n'inclut pas :

- les appels réseau réels au fournisseur MTN ;
- les identifiants ou secrets MTN réels ;
- l'intégration OAuth de production ;
- la mise en place d'une véritable passerelle de callback fournisseur.

## Décision d'architecture

La couche API MTN MoMo doit être conçue comme une couche de service résiliente, indépendante des intégrations bancaires ou opérateur réelles, afin de garantir :

- robustesse fonctionnelle ;
- traçabilité ;
- extensibilité ;
- conformité au modèle sandbox établi pour AfriWallet.
