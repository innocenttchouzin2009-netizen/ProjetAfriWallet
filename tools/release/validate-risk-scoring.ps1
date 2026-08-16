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
Write-Host 'AFW-DLV-0016.5 - Financial Risk Scoring Engine'

Invoke-Step 'Domain Build' {
    dotnet build backend/src/Compliance/RiskScoring.Domain/RiskScoring.Domain.csproj -c $Configuration -nologo
}
Invoke-Step 'Application Build' {
    dotnet build backend/src/Compliance/RiskScoring.Application/RiskScoring.Application.csproj -c $Configuration -nologo
}
Invoke-Step 'Infrastructure Build' {
    dotnet build backend/src/Compliance/RiskScoring.Infrastructure/RiskScoring.Infrastructure.csproj -c $Configuration -nologo
}
Invoke-Step 'API Build' {
    dotnet build backend/src/Compliance/RiskScoring.Api/RiskScoring.Api.csproj -c $Configuration -nologo
}
Invoke-Step 'Scenario Runner' {
    dotnet run --project backend/tests/RiskScoring.Scenarios/RiskScoring.Scenarios.csproj -c $Configuration
}
Invoke-Step 'Secret Scan' {
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-risk-scoring-secrets.ps1
}

Write-Host ''
Write-Host 'AFW-DLV-0016.5 VALIDATION PASS'
Write-Host 'Risk policy: SANDBOX'
Write-Host 'Source engines duplicated: NO'
Write-Host 'Regulatory/legal decision: NOT CLAIMED'
Write-Host 'Decision: READY FOR REVIEW'