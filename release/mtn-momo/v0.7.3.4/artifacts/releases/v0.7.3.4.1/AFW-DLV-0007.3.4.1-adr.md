# ADR — AFW-DLV-0007.3.4.1

**Titre :** Secure Configuration and Secret Management for MTN MoMo

## Décision

La configuration MTN MoMo sera gérée via une couche dédiée avec options validées, environnement spécifique et interface de secrets abstraite, afin de garantir un démarrage sûr, un contrôle fin des valeurs sensibles et une extensibilité vers des fournisseurs externes de secrets.

## Conséquences

- meilleure sécurité du démarrage
- séparation claire entre configuration et secrets
- architecture extensible pour Azure Key Vault, AWS Secrets Manager et Vault
