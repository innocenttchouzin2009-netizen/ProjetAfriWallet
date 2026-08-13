param(
    [ValidateSet('Debug', 'Release')]
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

Push-Location $PSScriptRoot

try {
    Invoke-Step 'Treasury Production Gate (AFW-DLV-0013.6)' {
        powershell -NoProfile -ExecutionPolicy Bypass -File .\validate-treasury-production.ps1 -Configuration $Configuration
    }

    Invoke-Step 'Treasury Disaster Recovery Gate (AFW-DLV-0013.7)' {
        powershell -NoProfile -ExecutionPolicy Bypass -File .\validate-treasury-disaster-recovery.ps1 -Configuration $Configuration
    }

    Invoke-Step 'Treasury RC Build' {
        dotnet build backend/src/FinancialPlatform/TreasuryReleaseCandidate/TreasuryReleaseCandidate.csproj -c $Configuration
    }

    Invoke-Step 'Treasury RC Validator' {
        dotnet run --project backend/src/FinancialPlatform/TreasuryReleaseCandidate/TreasuryReleaseCandidate.csproj -c $Configuration
    }

    Invoke-Step 'Treasury RC Scenarios' {
        dotnet run --project backend/tests/TreasuryReleaseCandidate.Scenarios/TreasuryReleaseCandidate.Scenarios.csproj -c $Configuration
    }

    Invoke-Step 'Treasury Secret Scan' {
        powershell -NoProfile -ExecutionPolicy Bypass -File .\scan-treasury-secrets.ps1
    }

    Write-Host ''
    Write-Host 'All AFW-DLV-0013.8 treasury release-candidate checks passed.'
}
finally {
    Pop-Location
}
