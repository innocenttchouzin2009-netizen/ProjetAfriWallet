param([string]$Configuration='Release')
$ErrorActionPreference='Stop';function Invoke-Step{param([string]$Name,[scriptblock]$Command)Write-Host "`n==================================================`n$Name`n==================================================";& $Command;if($LASTEXITCODE-ne 0){throw "$Name failed with exit code $LASTEXITCODE"};Write-Host "$Name PASS"}
Write-Host 'AFW-DLV-0016.8';Write-Host 'Compliance Platform Release Candidate v1.6.0-rc1'
Invoke-Step 'Frozen Delivery Verification' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/verify-compliance-frozen-deliveries.ps1 }
Invoke-Step '0016.7 Compliance Readiness' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/validate-compliance-readiness.ps1 -Configuration $Configuration }
Invoke-Step 'Compliance RC Build' { dotnet build backend/tests/ComplianceReleaseCandidate.Scenarios/ComplianceReleaseCandidate.Scenarios.csproj -c $Configuration -nologo }
Invoke-Step 'Compliance RC Runner' { dotnet run --project backend/tests/ComplianceReleaseCandidate.Scenarios/ComplianceReleaseCandidate.Scenarios.csproj -c $Configuration --no-build }
Invoke-Step 'RC Manifest' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/create-compliance-rc-manifest.ps1 }
Invoke-Step 'RC Package Verification' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/verify-compliance-rc-package.ps1 }
Invoke-Step 'Git Diff Verification' { git diff --check }
Write-Host 'AFW-DLV-0016.8 VALIDATION PASS';Write-Host 'Decision: READY FOR COMPLIANCE RC'