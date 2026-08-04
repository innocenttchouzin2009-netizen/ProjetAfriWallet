# AFW-DLV-0007.3.4.5 — PRD

## Objectif

Protéger les endpoints MTN MoMo contre les abus, rafales de requêtes, doublons rapides et surcharge du connecteur.

## Fonctionnalités

- policies ASP.NET Core Rate Limiting
- partitions par IP, AWID, WalletId, numéro normalisé, callback et connecteur
- réponses 429 normalisées avec CorrelationId
- exclusion des health checks
