# Validation Report

## Environment
- OS: Windows
- Runtime: .NET 10

## Verification
- Command: dotnet run --project backend/tests/RiskScoring.Scenarios/RiskScoring.Scenarios.csproj
- Result: PASS
- Command: dotnet build backend/src/RiskPlatform/RiskScoring.Api/RiskScoring.Api.csproj -c Release
- Result: PASS
