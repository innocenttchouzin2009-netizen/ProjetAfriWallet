param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Command)
    Write-Host "`n==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
    Write-Host "$Name PASS"
}

Write-Host "`nAFW-DLV-0019.2 - Merchant Onboarding & Verification Orchestration"
Invoke-Step 'Domain Build' { dotnet build backend/src/Merchants/MerchantOnboarding/MerchantOnboarding.Domain/MerchantOnboarding.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Merchants/MerchantOnboarding/MerchantOnboarding.Application/MerchantOnboarding.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Merchants/MerchantOnboarding/MerchantOnboarding.Infrastructure/MerchantOnboarding.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Merchants/MerchantOnboarding/MerchantOnboarding.Api/MerchantOnboarding.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/MerchantVerification.Scenarios/MerchantVerification.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-merchant-verification-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }

Write-Host "`nAFW-DLV-0019.2 VALIDATION PASS"
Write-Host 'Merchant onboarding: IMPLEMENTED'
Write-Host 'Document collection: IMPLEMENTED'
Write-Host 'Sandbox verification: IMPLEMENTED'
Write-Host 'Manual review: IMPLEMENTED'
Write-Host 'Payment acceptance: NOT IMPLEMENTED'
Write-Host 'Payment capture: NOT IMPLEMENTED'
Write-Host 'Settlement: NOT IMPLEMENTED'
Write-Host 'Payout: NOT IMPLEMENTED'
Write-Host 'Money movement: NOT IMPLEMENTED'
Write-Host 'Ledger mutation: NOT IMPLEMENTED'
Write-Host 'Decision: READY FOR REVIEW'
