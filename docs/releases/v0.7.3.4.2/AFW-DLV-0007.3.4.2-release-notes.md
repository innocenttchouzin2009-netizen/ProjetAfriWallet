# AFW-DLV-0007.3.4.2 — Release Notes

**Version :** v0.7.3.4.2  
**Sprint :** Sprint 7 — Mobile Money, Banking & Card Network  
**Type :** Health Checks & Readiness

## Objectif

Cette livraison transforme les vérifications de santé en signaux d'exploitation réels, avec des endpoints distincts pour la vivacité du processus, la disponibilité du service et la validité initiale des dépendances critiques.

## Nouvelles capacités

- endpoints /health/live, /health/ready et /health/startup
- probes distincts pour configuration, secrets, connecteur et readiness
- réponses JSON normalisées sans secrets
- séparation entre état Healthy et Degraded
- scénarios de validation automatisés

## Validation

```bash
dotnet build backend/src/MobileMoney/MobileMoney.Api/MobileMoney.Api.csproj -c Release
dotnet run --project backend/tests/MobileMoney.MtnMomo.Health.Scenarios/MobileMoney.MtnMomo.Health.Scenarios.csproj
```
