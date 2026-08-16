$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Compliance/TransactionMonitoring.Domain',
    'backend/src/Compliance/TransactionMonitoring.Application',
    'backend/src/Compliance/TransactionMonitoring.Infrastructure',
    'backend/src/Compliance/TransactionMonitoring.Api',
    'backend/tests/TransactionMonitoring.Scenarios',
    'docs/specs/aml-transaction-monitoring'
)

$patterns = @(
    'BEGIN PRIVATE KEY',
    'BEGIN RSA PRIVATE KEY',
    'ghp_',
    'github_pat_',
    'AML_API_KEY=',
    'AML_PROVIDER_SECRET=',
    'REGULATORY_REPORTING_SECRET='
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
    Write-Host 'AML secret scan FAIL'
    $violations | ForEach-Object { Write-Host $_ }
    exit 1
}

Write-Host 'AML secret scan PASS'