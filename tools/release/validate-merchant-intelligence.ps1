param([string]$Configuration='Release')
$ErrorActionPreference='Stop'
function Invoke-Step{param([string]$Name,[scriptblock]$Command)Write-Host "`n==> $Name";& $Command;if($LASTEXITCODE -ne 0){throw "$Name failed."};Write-Host "$Name PASS"}
Write-Host "`nAFW-DLV-0019.6 - Merchant Risk, Commerce Intelligence & Protection Engine"
Invoke-Step 'Domain Build'{dotnet build backend/src/Merchants/MerchantIntelligence/MerchantIntelligence.Domain/MerchantIntelligence.Domain.csproj -c $Configuration -nologo}
Invoke-Step 'Application Build'{dotnet build backend/src/Merchants/MerchantIntelligence/MerchantIntelligence.Application/MerchantIntelligence.Application.csproj -c $Configuration -nologo}
Invoke-Step 'Infrastructure Build'{dotnet build backend/src/Merchants/MerchantIntelligence/MerchantIntelligence.Infrastructure/MerchantIntelligence.Infrastructure.csproj -c $Configuration -nologo}
Invoke-Step 'API Build'{dotnet build backend/src/Merchants/MerchantIntelligence/MerchantIntelligence.Api/MerchantIntelligence.Api.csproj -c $Configuration -nologo}
Invoke-Step 'Scenario Runner'{dotnet run --project backend/tests/MerchantIntelligence.Scenarios/MerchantIntelligence.Scenarios.csproj -c $Configuration}
Invoke-Step 'Secret Scan'{powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-merchant-intelligence-secrets.ps1}
Invoke-Step 'Git Diff'{git diff --check}
Write-Host "`nAFW-DLV-0019.6 VALIDATION PASS";Write-Host 'Merchant risk scoring: IMPLEMENTED';Write-Host 'Commerce intelligence: IMPLEMENTED';Write-Host 'Pattern detection: IMPLEMENTED';Write-Host 'Explainability: IMPLEMENTED';Write-Host 'Protection recommendations: IMPLEMENTED';Write-Host 'Automatic merchant blocking: NOT IMPLEMENTED';Write-Host 'Automatic merchant suspension: NOT IMPLEMENTED';Write-Host 'Automatic settlement freeze: NOT IMPLEMENTED';Write-Host 'Automatic payout freeze: NOT IMPLEMENTED';Write-Host 'Payment capture: NOT IMPLEMENTED';Write-Host 'Money movement: NOT IMPLEMENTED';Write-Host 'Ledger mutation: NOT IMPLEMENTED';Write-Host 'Decision: READY FOR REVIEW'
