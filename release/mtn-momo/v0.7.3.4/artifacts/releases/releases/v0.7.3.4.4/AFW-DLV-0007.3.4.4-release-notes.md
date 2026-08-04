# AFW-DLV-0007.3.4.4 — Release Notes

## Objectif

Cette livraison ajoute une pipeline de résilience standardisée au backend MobileMoney, avec retry, timeout, circuit breaker et fallback contrôlé.

## Validation

```bash
dotnet build backend/src/MobileMoney/MobileMoney.Api/MobileMoney.Api.csproj -c Release
dotnet run --project backend/tests/MobileMoney.MtnMomo.Resilience.Scenarios/MobileMoney.MtnMomo.Resilience.Scenarios.csproj
```
