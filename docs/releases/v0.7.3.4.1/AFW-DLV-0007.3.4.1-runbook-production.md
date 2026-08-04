# Runbook Production — AFW-DLV-0007.3.4.1

## Vérifications pré-déploiement

- vérifier la présence des variables d'environnement requises
- vérifier que Production est désactivé par défaut
- vérifier que le diagnostic interne ne retourne pas de secrets

## En cas d'incident

1. vérifier l'état des variables d'environnement
2. vérifier les options de configuration
3. vérifier les secrets obligatoires
4. corriger la configuration puis redémarrer le service
