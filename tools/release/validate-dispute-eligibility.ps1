param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Command)
    Write-Host "`n==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
    Write-Host "$Name PASS"
}

Write-Host "`nAFW-DLV-0018.2 - Dispute Eligibility & Classification Engine"
Invoke-Step 'Domain Build' { dotnet build backend/src/Disputes/DisputeEligibility.Domain/DisputeEligibility.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Disputes/DisputeEligibility.Application/DisputeEligibility.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Disputes/DisputeEligibility.Infrastructure/DisputeEligibility.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Disputes/DisputeEligibility.Api/DisputeEligibility.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/DisputeEligibility.Scenarios/DisputeEligibility.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-dispute-eligibility-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }

Write-Host "`nAFW-DLV-0018.2 VALIDATION PASS"
Write-Host 'Eligibility: IMPLEMENTED'
Write-Host 'Classification: IMPLEMENTED'
Write-Host 'Refund decision: NOT IMPLEMENTED'
Write-Host 'Chargeback execution: NOT IMPLEMENTED'
Write-Host 'Money movement: NOT IMPLEMENTED'
Write-Host 'Decision: READY FOR REVIEW'
