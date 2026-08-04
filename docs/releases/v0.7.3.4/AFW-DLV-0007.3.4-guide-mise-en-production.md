# Guide de mise en production — AFW-DLV-0007.3.4

## Pré-requis

- environnement Staging validé
- configuration de secrets disponible
- observabilité active
- endpoints de santé accessibles

## Étapes de déploiement

1. Déployer la version avec la configuration validée.
2. Vérifier la disponibilité des health checks.
3. Vérifier l'envoi de traces et de logs structurés.
4. Activer progressivement les feature flags.
5. Contrôler l'impact sur les opérations MTN MoMo.

## Post-déploiement

- surveiller les métriques de latence, erreurs et saturation
- confirmer la bonne propagation des correlation IDs
- valider la conformité du rate limiting et du circuit breaker
- tenir à disposition le runbook et le plan de rollback
