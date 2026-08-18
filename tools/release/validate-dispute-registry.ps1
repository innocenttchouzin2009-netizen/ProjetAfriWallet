param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Command)
    Write-Host "`n==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
    Write-Host "$Name PASS"
}

Write-Host "`nAFW-DLV-0018.1 - Dispute Case & Claim Registry"
Invoke-Step 'Domain Build' { dotnet build backend/src/Disputes/DisputeRegistry.Domain/DisputeRegistry.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Disputes/DisputeRegistry.Application/DisputeRegistry.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Disputes/DisputeRegistry.Infrastructure/DisputeRegistry.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Disputes/DisputeRegistry.Api/DisputeRegistry.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/DisputeRegistry.Scenarios/DisputeRegistry.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-dispute-registry-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }

Write-Host "`nAFW-DLV-0018.1 VALIDATION PASS"
Write-Host 'Refund decision: NOT IMPLEMENTED'
Write-Host 'Chargeback execution: NOT IMPLEMENTED'
Write-Host 'Ledger mutation: NOT IMPLEMENTED'
Write-Host 'Decision: READY FOR REVIEW'
