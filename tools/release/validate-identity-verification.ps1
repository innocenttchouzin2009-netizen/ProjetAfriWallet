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
Write-Host 'AFW-DLV-0016.2 — Identity Verification Orchestration Platform'
Write-Host ''

Invoke-Step 'Domain Build' {
    dotnet build backend/src/Compliance/IdentityVerification.Domain/IdentityVerification.Domain.csproj -c $Configuration -nologo
}

Invoke-Step 'Application Build' {
    dotnet build backend/src/Compliance/IdentityVerification.Application/IdentityVerification.Application.csproj -c $Configuration -nologo
}

Invoke-Step 'Infrastructure Build' {
    dotnet build backend/src/Compliance/IdentityVerification.Infrastructure/IdentityVerification.Infrastructure.csproj -c $Configuration -nologo
}

Invoke-Step 'API Build' {
    dotnet build backend/src/Compliance/IdentityVerification.Api/IdentityVerification.Api.csproj -c $Configuration -nologo
}

Invoke-Step 'Scenario Runner' {
    dotnet run --project backend/tests/IdentityVerification.Scenarios/IdentityVerification.Scenarios.csproj -c $Configuration
}

Invoke-Step 'Secret Scan' {
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/scan-identity-verification-secrets.ps1
}

Write-Host ''
Write-Host 'AFW-DLV-0016.2 VALIDATION PASS'
Write-Host 'Providers: SANDBOX ONLY'
Write-Host 'Decision: READY FOR REVIEW'
