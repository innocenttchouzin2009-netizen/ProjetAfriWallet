# PRD — AFW-DLV-0007.3.4.1

**Titre :** Configuration & Secret Management

**Objectif :**
Créer une fondation de configuration sécurisée et réutilisable pour les intégrations MTN MoMo et les futurs connecteurs Mobile Money, banques et réseaux de cartes.

## Fonctionnalités attendues

- configuration MTN MoMo validée via IOptions<T>
- validation au démarrage avec ValidateOnStart()
- support des environnements Development, Staging et Production
- abstraction ISecretProvider
- implémentation par variables d'environnement
- cache court des secrets
- masquage systématique des valeurs sensibles
- contrôle des secrets obligatoires au démarrage
- feature flag empêchant toute activation accidentelle de la production
- endpoint interne de diagnostic sans exposition des secrets

## Critères d'acceptation

- l'application refuse de démarrer si une configuration obligatoire manque
- la production reste désactivée par défaut
- aucun secret n'est retourné par l'API
- aucun secret n'est écrit dans les logs
- les fichiers appsettings ne contiennent que les noms ou références des secrets

## Hors périmètre

- intégration réelle avec Azure Key Vault, AWS Secrets Manager ou HashiCorp Vault
- activation de production effective
