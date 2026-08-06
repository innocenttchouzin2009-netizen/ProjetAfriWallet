# Operations Guide — AFW-DLV-0011.8

## Scope
- Run the release candidate validation and review the generated evidence.
- Verify health, metrics, correlation, and protected internal endpoints.

## Operations
- Build the RC with `dotnet build backend/src/RiskPlatform/RiskPlatform.Production/RiskPlatform.Production.csproj -c Release`.
- Run the RC gate with `powershell -NoProfile -ExecutionPolicy Bypass -File build-risk-release.ps1 -Configuration Release`.
- Review the package under `release/risk-platform/v1.1.0-rc1/`.
