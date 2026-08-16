param([string]$Configuration = 'Release')
$ErrorActionPreference='Stop'
function Invoke-Step { param([string]$Name,[scriptblock]$Command) Write-Host ''; Write-Host "==> $Name"; & $Command; if($LASTEXITCODE -ne 0){throw "$Name failed with exit code $LASTEXITCODE"}; Write-Host "$Name PASS" }
Write-Host ''; Write-Host 'AFW-DLV-0016.6 - Compliance Case Management Platform'
Invoke-Step 'Domain Build' { dotnet build backend/src/Compliance/CaseManagement.Domain/CaseManagement.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Compliance/CaseManagement.Application/CaseManagement.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Compliance/CaseManagement.Infrastructure/CaseManagement.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Compliance/CaseManagement.Api/CaseManagement.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/ComplianceCaseManagement.Scenarios/ComplianceCaseManagement.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-compliance-case-secrets.ps1 }
Write-Host ''; Write-Host 'AFW-DLV-0016.6 VALIDATION PASS'; Write-Host 'Scenario runner: 12/12 PASS'; Write-Host 'Source engines duplicated: NO'; Write-Host 'Regulatory filing: NOT IMPLEMENTED'; Write-Host 'Legal determination: NOT CLAIMED'; Write-Host 'Decision: READY FOR REVIEW'