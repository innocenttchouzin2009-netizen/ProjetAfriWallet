# AFW-DLV-0007.3.4.5 — Release Notes

## Objectif

Cette livraison ajoute un système de rate limiting standardisé au backend MobileMoney, avec policies partitionnées et réponses 429 normalisées.

## Validation

```bash
dotnet build backend/src/MobileMoney/MobileMoney.Api/MobileMoney.Api.csproj -c Release
dotnet run --project backend/tests/MobileMoney.MtnMomo.RateLimiting.Scenarios/MobileMoney.MtnMomo.RateLimiting.Scenarios.csproj
```
