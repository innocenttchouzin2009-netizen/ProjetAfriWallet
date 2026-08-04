# Plan de rollback — AFW-DLV-0007.3.4

## Objectif

Décrire la procédure à suivre pour annuler ou rétablir une version déployée si la livraison production-readiness introduit un problème opérationnel.

## Conditions d'activation

- échec des health checks
- instabilité du service après déploiement
- erreurs de configuration critiques
- augmentation significative des erreurs métier ou techniques

## Procédure

1. Désactiver les feature flags concernés.
2. Revenir à la version précédente du service.
3. Vérifier la disponibilité des endpoints /health/live, /health/ready et /health/startup.
4. Vérifier la reprise des opérations métier normales.
5. Consigner l'incident et les actions de rollback.

## Points de vigilance

- préserver les secrets et la configuration actuelle
- vérifier les mécanismes d'audit et de logs après rollback
- s'assurer qu'aucune transaction critique n'est laissée en état incohérent
