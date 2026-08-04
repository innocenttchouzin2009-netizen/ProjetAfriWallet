# ADR — AFW-DLV-0007.3.4

**Titre :** Production Readiness for MTN MoMo Integration

## Statut

Proposed

## Contexte

La livraison MTN MoMo doit évoluer d'un module de démonstration vers une intégration de plateforme prête à être exploitée en production. Pour atteindre cette posture, il est nécessaire d'introduire des mécanismes de sécurité, d'observabilité, de résilience et de gouvernance opérationnelle au niveau de l'infrastructure applicative.

## Décision

Nous allons intégrer nativement dans la plateforme les composants suivants :

- configuration validée via IOptions<T> et ValidateOnStart()
- séparation des environnements Development, Staging et Production
- abstraction du secret provider
- health checks ASP.NET Core pour /health/live, /health/ready et /health/startup
- OpenTelemetry avec traces, métriques et propagation de contexte
- logging structuré JSON
- Polly v8 pour retry, timeout, circuit breaker et fallback
- middleware de rate limiting ASP.NET Core
- audit trail centralisé sans données sensibles
- feature flags pour l'activation progressive des fournisseurs

## Conséquences positives

- meilleure sécurité et conformité opérationnelle
- meilleure observabilité et diagnosabilité
- meilleure résilience face aux défauts externes
- préparation sérieuse à la mise en production

## Conséquences négatives

- complexité accrue de l'architecture
- coût de maintenance plus important
- besoin de surveillance et de gouvernance opérationnelle
