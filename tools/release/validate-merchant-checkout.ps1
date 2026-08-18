param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
function Invoke-Step { param([string]$Name, [scriptblock]$Command) Write-Host "`n==> $Name"; & $Command; if ($LASTEXITCODE -ne 0) { throw "$Name failed." }; Write-Host "$Name PASS" }
Write-Host "`nAFW-DLV-0019.3 - Checkout Session & Payment Intent Platform"
Invoke-Step 'Domain Build' { dotnet build backend/src/Merchants/MerchantCheckout/MerchantCheckout.Domain/MerchantCheckout.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Merchants/MerchantCheckout/MerchantCheckout.Application/MerchantCheckout.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Merchants/MerchantCheckout/MerchantCheckout.Infrastructure/MerchantCheckout.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Merchants/MerchantCheckout/MerchantCheckout.Api/MerchantCheckout.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/MerchantCheckout.Scenarios/MerchantCheckout.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-merchant-checkout-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }
Write-Host "`nAFW-DLV-0019.3 VALIDATION PASS"; Write-Host 'Checkout Session: IMPLEMENTED'; Write-Host 'Payment Intent: IMPLEMENTED'; Write-Host 'Idempotency: IMPLEMENTED'; Write-Host 'Token reference handling: IMPLEMENTED'; Write-Host 'Authorization: NOT IMPLEMENTED'; Write-Host 'Capture: NOT IMPLEMENTED'; Write-Host 'Settlement: NOT IMPLEMENTED'; Write-Host 'Payout: NOT IMPLEMENTED'; Write-Host 'Money movement: NOT IMPLEMENTED'; Write-Host 'Ledger mutation: NOT IMPLEMENTED'; Write-Host 'Decision: READY FOR REVIEW'
