$ErrorActionPreference = "Stop"

$release = "release/banking-platform/v1.5.0"

$requiredFiles = @(
    "validation-report.json",
    "validation-report.md",
    "manifest.json",
    "checksums.sha256"
)

$requiredDirectories = @(
    "runbooks",
    "configuration",
    "dashboards",
    "evidence",
    "rollback"
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $release $file
    if (-not (Test-Path $path)) {
        throw "Missing readiness file: $path"
    }
    Write-Host "$file ........ PASS"
}

foreach ($directory in $requiredDirectories) {
    $path = Join-Path $release $directory
    if (-not (Test-Path $path)) {
        throw "Missing readiness directory: $path"
    }
    Write-Host "$directory ........ PASS"
}

Write-Host ""
Write-Host "Banking readiness package VERIFIED"
