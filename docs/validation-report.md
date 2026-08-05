# Validation Report — AFW-DLV-0009.2

## Build
- Command: dotnet build backend/src/Merchant/Merchant.Api/Merchant.Api.csproj -c Release
- Result: Passed

## Scenarios
- Command: dotnet run --project backend/tests/Merchant.Onboarding.Scenarios/Merchant.Onboarding.Scenarios.csproj
- Result: Passed

## Notes
- Merchant onboarding, KYC approval, KYC rejection, and activation flows are working.
- The onboarding contract is provider-agnostic and can be extended with adapters later.
