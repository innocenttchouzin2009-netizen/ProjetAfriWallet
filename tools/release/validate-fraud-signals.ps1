param([string]$Configuration='Release')
$ErrorActionPreference='Stop';function Invoke-Step{param([string]$Name,[scriptblock]$Command)Write-Host "`n=== $Name ===";& $Command;if($LASTEXITCODE-ne 0){throw "$Name failed."};Write-Host "$Name PASS"}
Invoke-Step 'Fraud Domain Build' { dotnet build backend/src/Fraud/FraudSignals.Domain/FraudSignals.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Fraud Application Build' { dotnet build backend/src/Fraud/FraudSignals.Application/FraudSignals.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Fraud Infrastructure Build' { dotnet build backend/src/Fraud/FraudSignals.Infrastructure/FraudSignals.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'Fraud Scenario Build' { dotnet build backend/tests/FraudSignals.Scenarios/FraudSignals.Scenarios.csproj -c $Configuration -nologo }
Invoke-Step 'Fraud Scenario Runner' { dotnet run --project backend/tests/FraudSignals.Scenarios/FraudSignals.Scenarios.csproj -c $Configuration --no-build }
Invoke-Step 'Git Diff Validation' { git diff --check }
Write-Host 'AFW-DLV-0017.1 VALIDATION PASS';Write-Host 'Fraud decisioning: NOT IMPLEMENTED';Write-Host 'Payment blocking: NOT IMPLEMENTED';Write-Host 'AML/KYC engines: NOT DUPLICATED';Write-Host 'Decision: READY FOR REVIEW'