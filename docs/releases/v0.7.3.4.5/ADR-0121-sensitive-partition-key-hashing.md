# ADR-0121 — Sensitive Partition-Key Hashing

## Décision

Les clés de partition liées aux numéros de téléphone sont hachées avant d’être utilisées dans les limites de débit, afin d’éviter toute exposition de données sensibles dans les métriques ou les logs.
