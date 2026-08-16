$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Compliance/RiskScoring.Domain',
    'backend/src/Compliance/RiskScoring.Application',
    'backend/src/Compliance/RiskScoring.Infrastructure',
    'backend/src/Compliance/RiskScoring.Api',
    'backend/tests/RiskScoring.Scenarios',
    'docs/specs/financial-risk-scoring'
)
$patterns = @(
    'BEGIN PRIVATE KEY',
    'BEGIN RSA PRIVATE KEY',
    'ghp_',
    'github_pat_',
    'RISK_API_KEY=',
    'RISK_PROVIDER_SECRET=',
    'DECISION_ENGINE_SECRET='
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
                $match = Select-String -Path $_.FullName -Pattern $pattern -SimpleMatch -ErrorAction SilentlyContinue
                if ($match) {
                    $violations += "$($_.FullName) -> $pattern"
                }
            }
        }
}

if ($violations.Count -gt 0) {
    Write-Host 'Risk scoring secret scan FAIL'
    $violations | ForEach-Object { Write-Host $_ }
    exit 1
}

Write-Host 'Risk scoring secret scan PASS'