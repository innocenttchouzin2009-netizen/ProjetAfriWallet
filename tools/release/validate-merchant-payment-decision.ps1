param([string]$Configuration='Release')
$ErrorActionPreference='Stop'
function Invoke-Step {param([string]$Name,[scriptblock]$Command) Write-Host "`n==> $Name";& $Command;if($LASTEXITCODE -ne 0){throw "$Name failed."};Write-Host "$Name PASS"}
Write-Host "`nAFW-DLV-0019.4 - Merchant Payment Authorization & Capture Decision Engine"
Invoke-Step 'Domain Build' {dotnet build backend/src/Merchants/MerchantPaymentDecision/MerchantPaymentDecision.Domain/MerchantPaymentDecision.Domain.csproj -c $Configuration -nologo}
Invoke-Step 'Application Build' {dotnet build backend/src/Merchants/MerchantPaymentDecision/MerchantPaymentDecision.Application/MerchantPaymentDecision.Application.csproj -c $Configuration -nologo}
Invoke-Step 'Infrastructure Build' {dotnet build backend/src/Merchants/MerchantPaymentDecision/MerchantPaymentDecision.Infrastructure/MerchantPaymentDecision.Infrastructure.csproj -c $Configuration -nologo}
Invoke-Step 'API Build' {dotnet build backend/src/Merchants/MerchantPaymentDecision/MerchantPaymentDecision.Api/MerchantPaymentDecision.Api.csproj -c $Configuration -nologo}
Invoke-Step 'Scenario Runner' {dotnet run --project backend/tests/MerchantPaymentDecision.Scenarios/MerchantPaymentDecision.Scenarios.csproj -c $Configuration}
Invoke-Step 'Secret Scan' {powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-merchant-payment-decision-secrets.ps1}
Invoke-Step 'Git Diff' {git diff --check}
Write-Host "`nAFW-DLV-0019.4 VALIDATION PASS";Write-Host 'Authorization decision: IMPLEMENTED';Write-Host 'Step-up decision: IMPLEMENTED';Write-Host 'Capture eligibility decision: IMPLEMENTED';Write-Host 'Explainability: IMPLEMENTED';Write-Host 'Policy versioning: IMPLEMENTED';Write-Host 'Authorization execution: NOT IMPLEMENTED';Write-Host 'Capture execution: NOT IMPLEMENTED';Write-Host 'Settlement: NOT IMPLEMENTED';Write-Host 'Payout: NOT IMPLEMENTED';Write-Host 'Money movement: NOT IMPLEMENTED';Write-Host 'Ledger mutation: NOT IMPLEMENTED';Write-Host 'Decision: READY FOR REVIEW'
