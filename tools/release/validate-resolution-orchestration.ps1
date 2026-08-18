param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Command)
    Write-Host "`n==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
    Write-Host "$Name PASS"
}

Write-Host "`nAFW-DLV-0018.5 - Recovery & Resolution Orchestration Platform"
Invoke-Step 'Domain Build' { dotnet build backend/src/Disputes/ResolutionOrchestration/ResolutionOrchestration.Domain/ResolutionOrchestration.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Disputes/ResolutionOrchestration/ResolutionOrchestration.Application/ResolutionOrchestration.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Disputes/ResolutionOrchestration/ResolutionOrchestration.Infrastructure/ResolutionOrchestration.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Disputes/ResolutionOrchestration/ResolutionOrchestration.Api/ResolutionOrchestration.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/ResolutionOrchestration.Scenarios/ResolutionOrchestration.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-resolution-orchestration-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }

Write-Host "`nAFW-DLV-0018.5 VALIDATION PASS"
Write-Host 'Resolution orchestration: IMPLEMENTED'
Write-Host 'Refund routing: IMPLEMENTED'
Write-Host 'Chargeback routing: IMPLEMENTED'
Write-Host 'Idempotency: IMPLEMENTED'
Write-Host 'Retry policy: IMPLEMENTED'
Write-Host 'Compensation workflow: IMPLEMENTED'
Write-Host 'Real refund execution: NOT IMPLEMENTED'
Write-Host 'Real chargeback submission: NOT IMPLEMENTED'
Write-Host 'Money movement: NOT IMPLEMENTED'
Write-Host 'Direct ledger mutation: NOT IMPLEMENTED'
Write-Host 'Decision: READY FOR REVIEW'
