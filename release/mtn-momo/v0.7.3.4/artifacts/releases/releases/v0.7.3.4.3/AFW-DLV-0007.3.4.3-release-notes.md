# AFW-DLV-0007.3.4.3 — Release Notes

## Objectif

Cette livraison met en place une journalisation structurée et traçable pour les opérations MobileMoney, avec propagation de corrélation et redaction des données sensibles.

## Validation

```bash
dotnet build backend/src/MobileMoney/MobileMoney.Api/MobileMoney.Api.csproj -c Release
dotnet run --project backend/tests/MobileMoney.MtnMomo.Logging.Scenarios/MobileMoney.MtnMomo.Logging.Scenarios.csproj
```
