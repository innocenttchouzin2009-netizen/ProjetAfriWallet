param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

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

Write-Host ""
Write-Host "AFW-DLV-0015.8 - Banking Platform Release Candidate v1.5.0-rc1"

Invoke-Step "Banking Production Readiness" {
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/validate-banking-platform.ps1 -Configuration $Configuration
}

Invoke-Step "Banking RC Build" {
    dotnet build backend/src/BankingPlatform/BankingReleaseCandidate/BankingReleaseCandidate.csproj -c $Configuration -nologo
}

Invoke-Step "Banking RC Scenarios" {
    dotnet run --project backend/tests/BankingReleaseCandidate.Scenarios/BankingReleaseCandidate.Scenarios.csproj -c $Configuration
}

Invoke-Step "Banking RC Packaging" {
    dotnet run --project backend/src/BankingPlatform/BankingReleaseCandidate/BankingReleaseCandidate.csproj -c $Configuration
}

Invoke-Step "Banking RC Package Verification" {
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/release/verify-banking-rc.ps1
}

Write-Host ""
Write-Host "AFW-DLV-0015.8 COMPLETE"
Write-Host "Decision: READY FOR BANKING RC"
