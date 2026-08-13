param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host ''
Write-Host 'AFW-DLV-0014.8 - Payment Platform Release Candidate v1.4.0-rc1'
Write-Host ''

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

Invoke-Step "Payment Production Readiness" {

    powershell `
        -NoProfile `
        -ExecutionPolicy Bypass `
        -File .\validate-payment-platform.ps1 `
        -Configuration $Configuration
}

Invoke-Step "Payment RC Build" {

    dotnet build `
        backend/src/PaymentPlatform/PaymentReleaseCandidate/PaymentReleaseCandidate.csproj `
        -c $Configuration `
        -nologo
}

Invoke-Step "Payment RC Scenario Runner" {

    dotnet run `
        --project backend/tests/PaymentReleaseCandidate.Scenarios/PaymentReleaseCandidate.Scenarios.csproj `
        -c $Configuration
}

Invoke-Step "Payment RC Packaging" {

    dotnet run `
        --project backend/src/PaymentPlatform/PaymentReleaseCandidate/PaymentReleaseCandidate.csproj `
        -c $Configuration
}

Invoke-Step "Payment Secret Scan" {

    powershell `
        -NoProfile `
        -ExecutionPolicy Bypass `
        -File .\scan-payment-secrets.ps1
}

Write-Host ''
Write-Host 'AFW-DLV-0014.8 COMPLETE'
Write-Host 'Decision: READY FOR PAYMENT RC'
