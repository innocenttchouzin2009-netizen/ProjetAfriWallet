$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Compliance/Screening.Domain',
    'backend/src/Compliance/Screening.Application',
    'backend/src/Compliance/Screening.Infrastructure',
    'backend/src/Compliance/Screening.Api',
    'backend/tests/Screening.Scenarios',
    'docs/specs/sanctions-pep-screening'
)

$patterns = @(
    'BEGIN PRIVATE KEY',
    'BEGIN RSA PRIVATE KEY',
    'ghp_',
    'github_pat_',
    'SANCTIONS_API_KEY=',
    'PEP_API_KEY=',
    'SCREENING_PROVIDER_SECRET='
)

$violations = @()

foreach ($root in $roots) {
    if (-not (Test-Path $root)) {
        continue
    }

    Get-ChildItem -Path $root -Recurse -File |
        Where-Object {
            $_.FullName -notmatch '\\bin\\' -and
            $_.FullName -notmatch '\\obj\\'
        } |
        ForEach-Object {
            foreach ($pattern in $patterns) {
                $match = Select-String `
                    -Path $_.FullName `
                    -Pattern $pattern `
                    -SimpleMatch `
                    -ErrorAction SilentlyContinue
                if ($match) {
                    $violations += "$($_.FullName) -> $pattern"
                }
            }
        }
}

if ($violations.Count -gt 0) {
    Write-Host 'Screening secret scan FAIL'
    $violations | ForEach-Object { Write-Host $_ }
    exit 1
}

Write-Host 'Screening secret scan PASS'