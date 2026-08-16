param([string]$Configuration='Release')
$ErrorActionPreference='Stop'
function Invoke-Step{param([string]$Name,[scriptblock]$Command)Write-Host '';Write-Host "==> $Name";& $Command;if($LASTEXITCODE -ne 0){throw "$Name failed with exit code $LASTEXITCODE"};Write-Host "$Name PASS"}
Write-Host 'AFW-DLV-0016.7';Write-Host 'Compliance Platform Production Readiness'
Invoke-Step '0016.1 Compliance Profile API Restore' { dotnet restore backend/src/CompliancePlatform/ComplianceProfile.Api/ComplianceProfile.Api.csproj }
Invoke-Step '0016.1 Compliance Profile Gate' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/build-compliance-profile-release.ps1 -Configuration $Configuration }
Invoke-Step '0016.2 Identity Verification Gate' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/validate-identity-verification.ps1 -Configuration $Configuration }
Invoke-Step '0016.3 Screening Gate' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/validate-screening-engine.ps1 -Configuration $Configuration }
Invoke-Step '0016.4 Transaction Monitoring Gate' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/validate-transaction-monitoring.ps1 -Configuration $Configuration }
Invoke-Step '0016.5 Risk Scoring Gate' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/validate-risk-scoring.ps1 -Configuration $Configuration }
Invoke-Step '0016.6 Case Management Gate' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/validate-compliance-case-management.ps1 -Configuration $Configuration }
Invoke-Step 'Compliance Readiness Build' { dotnet build backend/tests/ComplianceReadiness.Scenarios/ComplianceReadiness.Scenarios.csproj -c $Configuration -nologo }
Invoke-Step 'Compliance Readiness Runner' { dotnet run --project backend/tests/ComplianceReadiness.Scenarios/ComplianceReadiness.Scenarios.csproj -c $Configuration --no-build }
Invoke-Step 'Architecture Boundary Gate' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/check-compliance-boundaries.ps1 }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-compliance-secrets.ps1 }
Invoke-Step 'Regulatory Claim Gate' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/check-compliance-regulatory-claims.ps1 }
Invoke-Step 'Git Diff Validation' { git diff --check }
Write-Host '';Write-Host 'AFW-DLV-0016.7 VALIDATION PASS';Write-Host 'Production provider certification: NOT CLAIMED';Write-Host 'Regulatory approval: NOT CLAIMED';Write-Host 'Decision: READY FOR COMPLIANCE RC'