# Checklist QA — AFW-DLV-0007.3.4

## Configuration

- [ ] les variables obligatoires sont définies
- [ ] les environnements sont séparés correctement
- [ ] les secrets sont chargés via le provider abstrait
- [ ] la validation de configuration échoue proprement en cas d'erreur

## Health Checks

- [ ] /health/live répond correctement
- [ ] /health/ready répond correctement
- [ ] /health/startup répond correctement

## Observabilité

- [ ] les traces sont émises
- [ ] les métriques sont collectées
- [ ] les logs structurés sont produits au format attendu
- [ ] l'ID de corrélation est présent

## Résilience

- [ ] le retry fonctionne correctement
- [ ] le timeout est appliqué
- [ ] le circuit breaker se déclenche comme prévu
- [ ] le fallback s'exécute en cas de panne
- [ ] le rate limiting limite bien les appels excessifs

## Audit et Feature Flags

- [ ] l'audit trail enregistre les actions critiques
- [ ] les données sensibles ne sont pas exposées
- [ ] les feature flags activent/désactivent correctement les fournisseurs
