param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Command)
    Write-Host "`n==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
    Write-Host "$Name PASS"
}

Write-Host "`nAFW-DLV-0018.4 - Refund & Chargeback Decision Engine"
Invoke-Step 'Domain Build' { dotnet build backend/src/Disputes/DisputeDecision/DisputeDecision.Domain/DisputeDecision.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Disputes/DisputeDecision/DisputeDecision.Application/DisputeDecision.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Disputes/DisputeDecision/DisputeDecision.Infrastructure/DisputeDecision.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Disputes/DisputeDecision/DisputeDecision.Api/DisputeDecision.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/DisputeDecision.Scenarios/DisputeDecision.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-dispute-decision-secrets.ps1 }
Invoke-Step 'Git Diff Check' { git diff --check }

Write-Host "`nAFW-DLV-0018.4 VALIDATION PASS"
Write-Host 'Refund decision: IMPLEMENTED'
Write-Host 'Chargeback decision: IMPLEMENTED'
Write-Host 'Refund execution: NOT IMPLEMENTED'
Write-Host 'Chargeback execution: NOT IMPLEMENTED'
Write-Host 'Money movement: NOT IMPLEMENTED'
Write-Host 'Ledger mutation: NOT IMPLEMENTED'
Write-Host 'Decision: READY FOR REVIEW'
