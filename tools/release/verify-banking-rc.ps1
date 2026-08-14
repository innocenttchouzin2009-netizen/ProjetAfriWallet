$ErrorActionPreference = "Stop"

$root = "release/banking-platform/v1.5.0-rc1"
$files = @(
    "validation-report.json",
    "validation-report.md",
    "release-notes.md",
    "changelog.md",
    "manifest.json",
    "checksums.sha256"
)

$directories = @(
    "openapi",
    "adr",
    "runbooks",
    "dashboards",
    "configuration",
    "evidence",
    "artifacts",
    "rollback"
)

foreach ($file in $files) {
    $path = Join-Path $root $file
    if (-not (Test-Path $path)) {
        throw "Missing RC file: $path"
    }
    Write-Host "$file ........ PASS"
}

foreach ($directory in $directories) {
    $path = Join-Path $root $directory
    if (-not (Test-Path $path)) {
        throw "Missing RC directory: $path"
    }
    Write-Host "$directory ........ PASS"
}

Write-Host ""
Write-Host "Banking RC package VERIFIED"
