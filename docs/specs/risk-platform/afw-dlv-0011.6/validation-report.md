# Validation Report — AFW-DLV-0011.6

## Verification
- Build: passed
- Scenario suite: passed

## Commands
- dotnet build backend/src/RiskPlatform/RegulatoryReporting.Api/RegulatoryReporting.Api.csproj -c Release
- dotnet run --project backend/tests/RegulatoryReporting.Scenarios/RegulatoryReporting.Scenarios.csproj

## Scenario Results
- report creation PASS
- case data aggregation PASS
- report generation PASS
- versioning PASS
- review workflow PASS
- approval workflow PASS
- submission history PASS
- json export PASS
- csv export PASS
- pdf export PASS
- checksum verification PASS
- invalid transition rejected PASS
- audit generation PASS
- telemetry generation PASS
