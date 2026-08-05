# Runbook Production — AFW-DLV-0007.3.4.2

## Vérifications

- vérifier /health/live
- vérifier /health/ready
- vérifier /health/startup
- confirmer la présence de checks Healthy ou Degraded sans secrets exposés

## En cas d'incident

1. analyser le check en échec
2. vérifier la configuration et le secret provider
3. corriger la cause racine
4. redémarrer si nécessaire
