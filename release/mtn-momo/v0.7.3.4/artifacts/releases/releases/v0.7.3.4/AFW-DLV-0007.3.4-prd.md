# PRD — AFW-DLV-0007.3.4

**Titre :** MTN MoMo Production Readiness (Enterprise)

**Objectif :**
Faire passer le module MTN MoMo d'une logique de preuve de concept à une plateforme enterprise, prête à être exploitée en production avec des composants de qualité industrielle.

## Contexte métier

AfriWallet doit offrir une expérience de paiement mobile robuste, sécurisée, observable et conforme aux exigences de fonctionnement d'une plateforme bancaire moderne. Cette livraison couvre la préparation opérationnelle et technique nécessaire à une mise en production fiable.

## User Story

> En tant qu'opérateur de plateforme AfriWallet,
> 
> je souhaite disposer d'un environnement MTN MoMo production-ready,
> 
> afin de garantir la sécurité, l'observabilité, la résilience et la conformité opérationnelle des intégrations.

## Fonctionnalités attendues

Le système doit permettre :

- d'implémenter une configuration sécurisée avec IOptions<T> et ValidateOnStart()
- de séparer les environnements Development, Staging et Production
- de charger les secrets via un fournisseur abstrait ISecretProvider
- d'exposer des health checks sur /health/live, /health/ready et /health/startup
- d'intégrer OpenTelemetry avec traces, métriques et propagation de contexte
- de produire des logs structurés JSON avec correlation et transaction identifiers
- d'appliquer des politiques de retry, timeout, circuit breaker et fallback avec Polly v8
- de limiter les appels via le middleware ASP.NET Core officiel
- d'enregistrer les actions critiques dans un audit trail sécurisé
- de piloter l'activation des fournisseurs via des feature flags
- de valider les paramètres critiques au démarrage et de refuser le lancement si nécessaire

## Critères d'acceptation

- la configuration est validée au démarrage et refusera le lancement en cas d'erreur critique
- les health checks sont disponibles et testent réellement les composants critiques
- les traces, métriques et logs structurés sont intégrés et configurables
- les mécanismes de protection (retry, timeout, circuit breaker, rate limiting) sont présents et configurables
- l'audit trail enregistre les actions sans exposer de données sensibles
- la documentation opérationnelle et de mise en production est fournie

## Hors périmètre

Cette livraison n'inclut pas :

- l'intégration avec des services MTN réels en production
- la gestion d'identifiants secrets sensibles dans le dépôt
- la certification réglementaire ou la conformité bancaire finale

## Décision d'architecture

La production-readiness doit être pensée comme une couche transverse de fiabilité, d'observabilité et de sécurité, qui s'appuie sur les composants .NET natifs et sur une architecture modulaire, afin de garantir :

- une montée en charge maîtrisée
- une observation fine des opérations
- une résilience correcte face aux pannes externes
- une préparation sérieuse à la mise en production
