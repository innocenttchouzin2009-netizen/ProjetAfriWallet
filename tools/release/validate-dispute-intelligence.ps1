param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Command)
    Write-Host "`n==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
    Write-Host "$Name PASS"
}

Write-Host "`nAFW-DLV-0018.6 - Customer Protection & Dispute Intelligence Engine"
Invoke-Step 'Domain Build' { dotnet build backend/src/Disputes/DisputeIntelligence/DisputeIntelligence.Domain/DisputeIntelligence.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Disputes/DisputeIntelligence/DisputeIntelligence.Application/DisputeIntelligence.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Disputes/DisputeIntelligence/DisputeIntelligence.Infrastructure/DisputeIntelligence.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Disputes/DisputeIntelligence/DisputeIntelligence.Api/DisputeIntelligence.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/DisputeIntelligence.Scenarios/DisputeIntelligence.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-dispute-intelligence-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }

Write-Host "`nAFW-DLV-0018.6 VALIDATION PASS"
Write-Host 'Dispute intelligence: IMPLEMENTED'
Write-Host 'Customer protection recommendations: IMPLEMENTED'
Write-Host 'Explainability: IMPLEMENTED'
Write-Host 'Deterministic policy: IMPLEMENTED'
Write-Host 'Automatic merchant blocking: NOT IMPLEMENTED'
Write-Host 'Automatic customer suspension: NOT IMPLEMENTED'
Write-Host 'Refund execution: NOT IMPLEMENTED'
Write-Host 'Money movement: NOT IMPLEMENTED'
Write-Host 'Ledger mutation: NOT IMPLEMENTED'
Write-Host 'Decision: READY FOR REVIEW'
