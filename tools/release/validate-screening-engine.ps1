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
Write-Host 'AFW-DLV-0016.3 - Sanctions & PEP Screening Engine'

Invoke-Step 'Domain Build' {
    dotnet build backend/src/Compliance/Screening.Domain/Screening.Domain.csproj -c $Configuration -nologo
}

Invoke-Step 'Application Build' {
    dotnet build backend/src/Compliance/Screening.Application/Screening.Application.csproj -c $Configuration -nologo
}

Invoke-Step 'Infrastructure Build' {
    dotnet build backend/src/Compliance/Screening.Infrastructure/Screening.Infrastructure.csproj -c $Configuration -nologo
}

Invoke-Step 'API Build' {
    dotnet build backend/src/Compliance/Screening.Api/Screening.Api.csproj -c $Configuration -nologo
}

Invoke-Step 'Scenario Runner' {
    dotnet run --project backend/tests/Screening.Scenarios/Screening.Scenarios.csproj -c $Configuration
}

Invoke-Step 'Secret Scan' {
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-screening-secrets.ps1
}

Write-Host ''
Write-Host 'AFW-DLV-0016.3 VALIDATION PASS'
Write-Host 'Screening sources: SANDBOX ONLY'
Write-Host 'Regulatory certification: NOT CLAIMED'
Write-Host 'Decision: READY FOR REVIEW'