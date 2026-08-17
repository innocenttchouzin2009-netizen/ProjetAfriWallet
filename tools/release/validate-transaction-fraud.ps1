param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed."
    }
    Write-Host "$Name PASS"
}

Write-Host ""
Write-Host "AFW-DLV-0017.3 - Transaction Fraud Detection Engine"

Invoke-Step 'Domain Build' { dotnet build backend/src/Fraud/TransactionFraud.Domain/TransactionFraud.Domain.csproj -c $Configuration -nologo }
Invoke-Step 'Application Build' { dotnet build backend/src/Fraud/TransactionFraud.Application/TransactionFraud.Application.csproj -c $Configuration -nologo }
Invoke-Step 'Infrastructure Build' { dotnet build backend/src/Fraud/TransactionFraud.Infrastructure/TransactionFraud.Infrastructure.csproj -c $Configuration -nologo }
Invoke-Step 'API Build' { dotnet build backend/src/Fraud/TransactionFraud.Api/TransactionFraud.Api.csproj -c $Configuration -nologo }
Invoke-Step 'Scenario Runner' { dotnet run --project backend/tests/TransactionFraud.Scenarios/TransactionFraud.Scenarios.csproj -c $Configuration }
Invoke-Step 'Secret Scan' { powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-transaction-fraud-secrets.ps1 }
Invoke-Step 'Git Diff' { git diff --check }

Write-Host ""
Write-Host "AFW-DLV-0017.3 VALIDATION PASS"
Write-Host "Automatic payment decline: NOT IMPLEMENTED"
Write-Host "Payment mutation: NOT IMPLEMENTED"
Write-Host "Decision: READY FOR REVIEW"
