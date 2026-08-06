# CI/CD Guide — AFW-DLV-0011.7

## Pipeline Order
1. Restore and build RiskPlatform production host.
2. Run `validate-risk-platform.ps1` in Release mode.
3. Publish `release/risk-platform/v1.1.0/*` as pipeline artifacts.
4. Block merge if any check fails or reports are missing.

## Required Commands
- `dotnet build backend/src/RiskPlatform/RiskPlatform.Production/RiskPlatform.Production.csproj -c Release`
- `powershell -NoProfile -ExecutionPolicy Bypass -File validate-risk-platform.ps1 -Configuration Release`

## Merge Policy
- Target branch: `feature/risk-production-readiness` into `main`.
- Strategy: squash and merge only after validation artifacts are attached.
