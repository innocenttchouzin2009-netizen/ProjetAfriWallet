param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
function Invoke-Step {
    param([string]$Name, [scriptblock]$Command)
    Write-Host "`n==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
    Write-Host "$Name PASS"
}
Write-Host "`nAFW-DLV-0017.6 - Fraud Intelligence & Pattern Correlation Engine"
Invoke-Step 'Domain Build' { dotnet build backend/src/Fraud/FraudIntelligence.Domain/FraudIntelligence.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Fraud/FraudIntelligence.Application/FraudIntelligence.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Fraud/FraudIntelligence.Infrastructure/FraudIntelligence.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Fraud/FraudIntelligence.Api/FraudIntelligence.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/FraudIntelligence.Scenarios/FraudIntelligence.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-fraud-intelligence-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }
Write-Host "`nAFW-DLV-0017.6 VALIDATION PASS"
Write-Host 'Correlation engine: DETERMINISTIC'
Write-Host 'Explainability: REQUIRED'
Write-Host 'Machine learning: NOT IMPLEMENTED'
Write-Host 'Enforcement: NOT IMPLEMENTED'
Write-Host 'Decision: READY FOR REVIEW'