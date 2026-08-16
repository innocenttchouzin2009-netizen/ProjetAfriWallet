param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Command
    )

    Write-Host ''
    Write-Host "==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
    Write-Host "$Name PASS"
}

Write-Host ''
Write-Host 'AFW-DLV-0016.4 - AML Transaction Monitoring Engine'

Invoke-Step 'Domain Build' {
    dotnet build backend/src/Compliance/TransactionMonitoring.Domain/TransactionMonitoring.Domain.csproj -c $Configuration -nologo
}

Invoke-Step 'Application Build' {
    dotnet build backend/src/Compliance/TransactionMonitoring.Application/TransactionMonitoring.Application.csproj -c $Configuration -nologo
}

Invoke-Step 'Infrastructure Build' {
    dotnet build backend/src/Compliance/TransactionMonitoring.Infrastructure/TransactionMonitoring.Infrastructure.csproj -c $Configuration -nologo
}

Invoke-Step 'API Build' {
    dotnet build backend/src/Compliance/TransactionMonitoring.Api/TransactionMonitoring.Api.csproj -c $Configuration -nologo
}

Invoke-Step 'Scenario Runner' {
    dotnet run --project backend/tests/TransactionMonitoring.Scenarios/TransactionMonitoring.Scenarios.csproj -c $Configuration
}

Invoke-Step 'Secret Scan' {
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-transaction-monitoring-secrets.ps1
}

Write-Host ''
Write-Host 'AFW-DLV-0016.4 VALIDATION PASS'
Write-Host 'AML policy: SANDBOX'
Write-Host 'Regulatory filing: NOT IMPLEMENTED'
Write-Host 'Regulatory certification: NOT CLAIMED'
Write-Host 'Decision: READY FOR REVIEW'