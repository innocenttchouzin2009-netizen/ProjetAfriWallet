# PRD — AFW-DLV-0012.3

## Summary
Build an Operations & Back Office Portal to centralize internal supervision of core AfriWallet domains with granular permissions and strong controls for sensitive actions.

## Scope
- Dashboard and health overview.
- Global search by AWID, transaction, wallet, card, beneficiary, and merchant.
- User, transaction, wallet, card, and support case inspection.
- Wallet suspension, card freeze, support case assignment, and controlled transaction retry.
- Audit and telemetry for critical actions.

## Validation Targets
- dotnet build backend/src/OperationsPlatform/Operations.Api/Operations.Api.csproj -c Release
- dotnet run --project backend/tests/Operations.Scenarios/Operations.Scenarios.csproj
