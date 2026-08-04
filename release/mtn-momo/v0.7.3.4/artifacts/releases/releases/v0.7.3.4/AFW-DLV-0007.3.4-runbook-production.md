# Runbook Production — AFW-DLV-0007.3.4

## Objectif

Ce runbook décrit les opérations de surveillance, d'intervention et de maintenance de la couche production-readiness MTN MoMo.

## Pré-requis

- accès au serveur ou au cluster de déploiement
- accès aux logs centralisés
- accès aux dashboards OpenTelemetry / Grafana / Tempo / Azure Monitor
- accès au système de secrets

## Vérifications quotidiennes

1. Vérifier l'état des health checks.
2. Contrôler la disponibilité des endpoints /health/live, /health/ready et /health/startup.
3. Vérifier l'absence d'erreurs répétées sur retry, circuit breaker ou rate limiting.
4. Examiner les logs structurés et les traces distribuées.

## Procédures d'intervention

### Incident sur configuration

- vérifier les variables obligatoires
- valider les options de configuration
- vérifier le secret provider
- redémarrer seulement si la configuration a été corrigée

### Incident sur health checks

- identifier la vérification en échec
- vérifier l'état du connecteur sandbox, du stockage d'idempotence et du tracker
- corriger la cause racine avant toute nouvelle mise en service

### Incident sur latence ou saturation

- vérifier le circuit breaker
- vérifier les limites de rate limiting
- augmenter ou ajuster la stratégie si nécessaire
- documenter l'incident dans le journal d'exploitation

## Escalade

- incident critique : équipe plateforme et équipe intégration paiements
- incident récurrent : escalade produit et sécurité

## Nettoyage et suivi

- enregistrer les actions menées
- conserver les corrélations et IDs de transaction nécessaires au support
- tenir un journal des incidents et des correctifs
