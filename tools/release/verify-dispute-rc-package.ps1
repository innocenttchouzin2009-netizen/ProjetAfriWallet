$ErrorActionPreference = 'Stop'

$root = 'release/dispute-platform/v1.8.0-rc1'

$requiredFiles = @(
    'release-notes.md',
    'changelog.md',
    'validation-report.json',
    'validation-report.md',
    'manifest.sha256',
    'delivery-tags.txt'
)
$requiredDirectories = @(
    'runbooks',
    'evidence',
    'configuration',
    'rollback',
    'artifacts'
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $root $file
    if (-not (Test-Path $path)) {
        throw "Missing Dispute RC file: $path"
    }
    Write-Host "$file PASS"
}

foreach ($directory in $requiredDirectories) {
    $path = Join-Path $root $directory
    if (-not (Test-Path $path)) {
        throw "Missing Dispute RC directory: $path"
    }
    Write-Host "$directory PASS"
}

$report = Get-Content "$root/validation-report.json" -Raw | ConvertFrom-Json
if ($report.failed -ne 0) {
    throw 'Dispute RC contains failed checks.'
}
if ($report.skipped -ne 0) {
    throw 'Dispute RC contains skipped checks.'
}
if ($report.decision -ne 'READY FOR DISPUTE RC') {
    throw 'Dispute RC decision is not READY.'
}

$tags = Get-Content "$root/delivery-tags.txt"
if ($tags.Count -ne 7) {
    throw 'Expected 7 frozen delivery tags.'
}

Write-Host ''
Write-Host 'Dispute RC package VERIFIED'
