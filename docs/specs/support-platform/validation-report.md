# Validation Report — AFW-DLV-0012.2

Build command:
- dotnet build backend/src/SupportPlatform/Support.Api/Support.Api.csproj -c Release

Scenario command:
- dotnet run --project backend/tests/Support.Scenarios/Support.Scenarios.csproj

Expected result:
- case creation PASS
- automatic assignment PASS
- manual reassignment PASS
- customer message PASS
- internal note visibility PASS
- attachment validation PASS
- sla calculation PASS
- sla warning PASS
- sla breach PASS
- escalation flow PASS
- resolution flow PASS
- case closure PASS
- case reopening PASS
- timeline generation PASS
- notification integration PASS
- audit generation PASS
- telemetry generation PASS
