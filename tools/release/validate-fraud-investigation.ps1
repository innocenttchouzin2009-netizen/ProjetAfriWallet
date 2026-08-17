param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
function Invoke-Step {
    param([string]$Name, [scriptblock]$Command)
    Write-Host "`n==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
    Write-Host "$Name PASS"
}
Write-Host "`nAFW-DLV-0017.5 - Fraud Investigation & Response Platform"
Invoke-Step 'Domain Build' { dotnet build backend/src/Fraud/FraudInvestigation.Domain/FraudInvestigation.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Fraud/FraudInvestigation.Application/FraudInvestigation.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Fraud/FraudInvestigation.Infrastructure/FraudInvestigation.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Fraud/FraudInvestigation.Api/FraudInvestigation.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/FraudInvestigation.Scenarios/FraudInvestigation.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-fraud-investigation-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }
Write-Host "`nAFW-DLV-0017.5 VALIDATION PASS"
Write-Host 'Payment mutation: NOT IMPLEMENTED'
Write-Host 'Account restriction execution: NOT IMPLEMENTED'
Write-Host 'Device revocation execution: NOT IMPLEMENTED'
Write-Host 'Decision: READY FOR REVIEW'