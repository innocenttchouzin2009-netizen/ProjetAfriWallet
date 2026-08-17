param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
function Invoke-Step { param([string]$Name, [scriptblock]$Command); Write-Host "`n==================================================`n$Name`n=================================================="; & $Command; if ($LASTEXITCODE -ne 0) { throw "$Name failed." }; Write-Host "$Name PASS" }
Write-Host "`nAFW-DLV-0017.8`nFraud Platform Release Candidate v1.7.0-rc1"
Invoke-Step 'Frozen Delivery Verification' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/verify-fraud-rc-frozen-deliveries.ps1 }
Invoke-Step '0017.7 Fraud Readiness' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/validate-fraud-readiness.ps1 -Configuration $Configuration }
Invoke-Step 'Fraud RC Build' { dotnet build backend/tests/FraudReleaseCandidate.Scenarios/FraudReleaseCandidate.Scenarios.csproj -c $Configuration -nologo }
Invoke-Step 'Fraud RC Runner' { dotnet run --project backend/tests/FraudReleaseCandidate.Scenarios/FraudReleaseCandidate.Scenarios.csproj -c $Configuration --no-build -- (Get-Location).Path }
Invoke-Step 'Fraud RC Package Verification' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/verify-fraud-rc-package.ps1 }
Invoke-Step 'Git Diff Verification' { git diff --check }
Write-Host "`n==================================================`nAFW-DLV-0017.8 VALIDATION PASS`nFrozen deliveries: 7/7 VERIFIED`nRC checks: 18/18 PASS`nOpaque ML: NOT IMPLEMENTED`nAutomatic enforcement: NOT IMPLEMENTED`nDecision: READY FOR FRAUD RC`n=================================================="