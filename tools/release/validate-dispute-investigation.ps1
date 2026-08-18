param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Command)
    Write-Host "`n==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
    Write-Host "$Name PASS"
}

Write-Host "`nAFW-DLV-0018.3 - Evidence & Investigation Platform"
Invoke-Step 'Domain Build' { dotnet build backend/src/Disputes/DisputeInvestigation.Domain/DisputeInvestigation.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Disputes/DisputeInvestigation.Application/DisputeInvestigation.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Disputes/DisputeInvestigation.Infrastructure/DisputeInvestigation.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Disputes/DisputeInvestigation.Api/DisputeInvestigation.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/DisputeInvestigation.Scenarios/DisputeInvestigation.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-dispute-investigation-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }

Write-Host "`nAFW-DLV-0018.3 VALIDATION PASS"
Write-Host 'Evidence collection: IMPLEMENTED'
Write-Host 'Investigation lifecycle: IMPLEMENTED'
Write-Host 'Refund decision: NOT IMPLEMENTED'
Write-Host 'Chargeback execution: NOT IMPLEMENTED'
Write-Host 'Money movement: NOT IMPLEMENTED'
Write-Host 'Decision: READY FOR REVIEW'
