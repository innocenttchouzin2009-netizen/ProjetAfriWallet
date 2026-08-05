# Release Notes v0.7.4.1

## AFW-DLV-0007.4.1 — Bank Registry & Routing

### Highlights
- Introduced a banking registry module under backend/src/Banking
- Added provider seed data and deterministic routing logic
- Added REST endpoints for registry lookup and routing
- Added scenario runner for validation

### Validation
- dotnet build backend/src/Banking/Banking.Api/Banking.Api.csproj
- dotnet run --project backend/tests/Banking.Registry.Scenarios/Banking.Registry.Scenarios.csproj
