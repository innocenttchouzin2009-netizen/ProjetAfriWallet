param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Command
    )

    Write-Host "`n==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
    Write-Host "$Name PASS"
}

Write-Host "`nAFW-DLV-0017.4 - Fraud Rules & Decision Engine"
Invoke-Step 'Domain Build' { dotnet build backend/src/Fraud/FraudDecision.Domain/FraudDecision.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Fraud/FraudDecision.Application/FraudDecision.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Fraud/FraudDecision.Infrastructure/FraudDecision.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Fraud/FraudDecision.Api/FraudDecision.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/FraudDecision.Scenarios/FraudDecision.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-fraud-decision-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }

Write-Host "`nAFW-DLV-0017.4 VALIDATION PASS"
Write-Host 'Payment mutation: NOT IMPLEMENTED'
Write-Host 'Wallet suspension: NOT IMPLEMENTED'
Write-Host 'Device revocation: NOT IMPLEMENTED'
Write-Host 'Decision: READY FOR REVIEW'