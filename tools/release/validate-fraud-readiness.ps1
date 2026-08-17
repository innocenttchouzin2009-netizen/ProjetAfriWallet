param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
function Invoke-Step { param([string]$Name, [scriptblock]$Command); Write-Host "`n==================================================`n$Name`n=================================================="; & $Command; if ($LASTEXITCODE -ne 0) { throw "$Name failed." }; Write-Host "$Name PASS" }
Write-Host "`nAFW-DLV-0017.7 - Fraud Platform Production Readiness"
Invoke-Step 'Frozen Delivery Verification' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/verify-fraud-frozen-deliveries.ps1 }
Invoke-Step 'Readiness Build' { dotnet build backend/tests/FraudReadiness.Scenarios/FraudReadiness.Scenarios.csproj -c $Configuration -nologo }
Invoke-Step 'Readiness Runner' { dotnet run --project backend/tests/FraudReadiness.Scenarios/FraudReadiness.Scenarios.csproj -c $Configuration --no-build -- (Get-Location).Path }
Invoke-Step 'Execution Boundary' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/check-fraud-execution-boundaries.ps1 }
Invoke-Step 'Machine Learning Boundary' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/check-fraud-ml-boundary.ps1 }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-fraud-platform-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }
Write-Host "`n==================================================`nAFW-DLV-0017.7 VALIDATION PASS`nFrozen deliveries: 6/6 VERIFIED`nReadiness checks: 7/7 PASS`nMachine learning: NOT IMPLEMENTED`nAutomatic enforcement: NOT IMPLEMENTED`nDecision: READY FOR FRAUD RC`n=================================================="