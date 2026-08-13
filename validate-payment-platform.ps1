param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$releaseRoot = Join-Path $repoRoot 'release/payment-platform/v1.4.0-rc1'
$projectPath = Join-Path $repoRoot 'backend/src/PaymentPlatform/PaymentReleaseCandidate/PaymentReleaseCandidate.csproj'

if (-not (Test-Path -Path $projectPath)) {
    throw "Missing project: $projectPath"
}

Push-Location $repoRoot
try {
    & dotnet build $projectPath -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Payment platform validation build failed with exit code $LASTEXITCODE"
    }

    $requiredFiles = @(
        'validation-report.json',
        'validation-report.md',
        'release-notes.md',
        'changelog.md',
        'manifest.json',
        'checksums.sha256'
    )

    foreach ($file in $requiredFiles) {
        $target = Join-Path $releaseRoot $file
        if (-not (Test-Path -Path $target)) {
            throw "Missing RC artifact: $file"
        }
    }

    Write-Host 'Payment Platform validation PASS'
}
finally {
    Pop-Location
}
