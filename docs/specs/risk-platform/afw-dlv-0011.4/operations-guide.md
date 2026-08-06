# Operations Guide — AFW-DLV-0011.4

## Deployment
- Build the API with `dotnet build backend/src/RiskPlatform/Device.Api/Device.Api.csproj -c Release`.
- Run the scenario harness with `dotnet run --project backend/tests/Device.Scenarios/Device.Scenarios.csproj`.

## Monitoring
- Monitor the `Decision`, `RiskLevel`, `Score`, and `TriggeredSignalCount` fields.
- Alert when compromised or high-risk decisions exceed expected thresholds.
