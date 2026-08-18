param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Command)
    Write-Host "`n==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
    Write-Host "$Name PASS"
}

Write-Host "`nAFW-DLV-0019.1 - Merchant Registry & Business Profile Platform"
Invoke-Step 'Domain Build' { dotnet build backend/src/Merchants/MerchantRegistry.Domain/MerchantRegistry.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Merchants/MerchantRegistry.Application/MerchantRegistry.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Merchants/MerchantRegistry.Infrastructure/MerchantRegistry.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Merchants/MerchantRegistry.Api/MerchantRegistry.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/MerchantRegistry.Scenarios/MerchantRegistry.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-merchant-registry-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }

Write-Host "`nAFW-DLV-0019.1 VALIDATION PASS"
Write-Host 'Merchant Registry: IMPLEMENTED'
Write-Host 'Business Profile: IMPLEMENTED'
Write-Host 'Merchant Lifecycle: IMPLEMENTED'
Write-Host 'Capability Declaration: IMPLEMENTED'
Write-Host 'Audit: IMPLEMENTED'
Write-Host 'KYB Verification: NOT IMPLEMENTED'
Write-Host 'Payment Acceptance: NOT IMPLEMENTED'
Write-Host 'Payment Capture: NOT IMPLEMENTED'
Write-Host 'Settlement: NOT IMPLEMENTED'
Write-Host 'Payout: NOT IMPLEMENTED'
Write-Host 'Money Movement: NOT IMPLEMENTED'
Write-Host 'Ledger Mutation: NOT IMPLEMENTED'
Write-Host 'Decision: READY FOR REVIEW'
