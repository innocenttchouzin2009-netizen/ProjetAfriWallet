param([string]$Configuration='Release')
$ErrorActionPreference='Stop'
function Invoke-Step{param([string]$Name,[scriptblock]$Command)Write-Host "`n==> $Name";& $Command;if($LASTEXITCODE -ne 0){throw "$Name failed."};Write-Host "$Name PASS"}
Write-Host "`nAFW-DLV-0019.5 - Merchant Settlement & Payout Orchestration Platform"
Invoke-Step 'Domain Build'{dotnet build backend/src/Merchants/MerchantSettlement/MerchantSettlement.Domain/MerchantSettlement.Domain.csproj -c $Configuration -nologo}
Invoke-Step 'Application Build'{dotnet build backend/src/Merchants/MerchantSettlement/MerchantSettlement.Application/MerchantSettlement.Application.csproj -c $Configuration -nologo}
Invoke-Step 'Infrastructure Build'{dotnet build backend/src/Merchants/MerchantSettlement/MerchantSettlement.Infrastructure/MerchantSettlement.Infrastructure.csproj -c $Configuration -nologo}
Invoke-Step 'API Build'{dotnet build backend/src/Merchants/MerchantSettlement/MerchantSettlement.Api/MerchantSettlement.Api.csproj -c $Configuration -nologo}
Invoke-Step 'Scenario Runner'{dotnet run --project backend/tests/MerchantSettlementOrchestration.Scenarios/MerchantSettlementOrchestration.Scenarios.csproj -c $Configuration}
Invoke-Step 'Secret Scan'{powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-merchant-settlement-secrets.ps1}
Invoke-Step 'Git Diff'{git diff --check}
Write-Host "`nAFW-DLV-0019.5 VALIDATION PASS";Write-Host 'Settlement orchestration: IMPLEMENTED';Write-Host 'Payout orchestration: IMPLEMENTED';Write-Host 'Idempotency: IMPLEMENTED';Write-Host 'Retry policy: IMPLEMENTED';Write-Host 'Compensation workflow: IMPLEMENTED';Write-Host 'Provider correlation: IMPLEMENTED';Write-Host 'Real capture: NOT IMPLEMENTED';Write-Host 'Real settlement: NOT IMPLEMENTED';Write-Host 'Real payout: NOT IMPLEMENTED';Write-Host 'Money movement: NOT IMPLEMENTED';Write-Host 'Wallet mutation: NOT IMPLEMENTED';Write-Host 'Ledger mutation: NOT IMPLEMENTED';Write-Host 'Decision: READY FOR REVIEW'
