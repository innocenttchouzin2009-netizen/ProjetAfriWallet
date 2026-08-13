param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "AFW-DLV-0012.8 - Operations Platform RC"
Write-Host "Configuration: $Configuration"
Write-Host ""

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "==> $Name"

    & $Command

    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }

    Write-Host "$Name PASS"
}

Invoke-Step "Operations Readiness Validation" {
    powershell `
        -NoProfile `
        -ExecutionPolicy Bypass `
    -File .\validate-operations-platform.ps1
}

Invoke-Step "Secret Scan" {
    powershell `
        -NoProfile `
        -ExecutionPolicy Bypass `
        -File .\scan-operations-secrets.ps1
}

Invoke-Step "Release Candidate Build" {
    dotnet build `
        backend/src/OperationsPlatform/ReleaseCandidate/Operations.ReleaseCandidate.csproj `
        -c $Configuration
}

Invoke-Step "Release Candidate Packaging" {
    dotnet run `
        --project backend/src/OperationsPlatform/ReleaseCandidate/Operations.ReleaseCandidate.csproj `
        -c $Configuration
}

Write-Host ""
Write-Host "All AFW-DLV-0012.8 release candidate checks passed."
