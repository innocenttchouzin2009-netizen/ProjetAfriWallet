# PRD — AFW-DLV-0007.3.4.2

**Titre :** Health Checks & Readiness

**Objectif :**
Créer une couche de surveillance opérationnelle fiable pour MTN MoMo, avec des signaux distincts de liveness, readiness et startup.

## Fonctionnalités attendues

- /health/live, /health/ready et /health/startup
- vérification de la configuration MTN
- vérification du fournisseur de secrets
- vérification du connecteur
- vérification du readiness du service
- réponses JSON normalisées
- absence de secrets dans les réponses publiques

## Critères d'acceptation

- /health/live ne dépend pas d'un service externe
- /health/ready échoue si une dépendance critique est indisponible
- /health/startup confirme que la configuration initiale est exploitable
- les réponses publiques ne contiennent ni clé ni secret
