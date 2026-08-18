param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Command)
    Write-Host ''
    Write-Host $Name
    Write-Host '=================================================='
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
    Write-Host "$Name PASS"
}

Write-Host ''
Write-Host 'AFW-DLV-0018.7 - Dispute Platform Production Readiness'

Invoke-Step 'Frozen Delivery Verification' {
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/verify-dispute-frozen-deliveries.ps1
}
Invoke-Step 'Readiness Build' {
    dotnet build backend/tests/DisputeReadiness.Scenarios/DisputeReadiness.Scenarios.csproj -c $Configuration -nologo
}
Invoke-Step 'Readiness Runner' {
    dotnet run --project backend/tests/DisputeReadiness.Scenarios/DisputeReadiness.Scenarios.csproj -c $Configuration --no-build -- (Get-Location).Path
}
Invoke-Step 'Financial Boundary' {
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/check-dispute-financial-boundaries.ps1
}
Invoke-Step 'Ledger Boundary' {
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/check-dispute-ledger-boundary.ps1
}
Invoke-Step 'Secret Scan' {
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-dispute-platform-secrets.ps1
}
Invoke-Step 'Git Diff' {
    git diff --check
}

Write-Host ''
Write-Host '=================================================='
Write-Host 'AFW-DLV-0018.7 VALIDATION PASS'
Write-Host 'Frozen dispute deliveries: 6/6 VERIFIED'
Write-Host 'Readiness checks: 8/8 PASS'
Write-Host 'Real refund execution: NOT IMPLEMENTED'
Write-Host 'Real chargeback submission: NOT IMPLEMENTED'
Write-Host 'Automatic merchant blocking: NOT IMPLEMENTED'
Write-Host 'Automatic customer suspension: NOT IMPLEMENTED'
Write-Host 'Direct ledger mutation: NOT IMPLEMENTED'
Write-Host 'Decision: READY FOR DISPUTE RC'
Write-Host '=================================================='
