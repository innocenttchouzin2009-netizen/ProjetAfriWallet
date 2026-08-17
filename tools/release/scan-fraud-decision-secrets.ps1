$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Fraud/FraudDecision.Domain',
    'backend/src/Fraud/FraudDecision.Application',
    'backend/src/Fraud/FraudDecision.Infrastructure',
    'backend/src/Fraud/FraudDecision.Api',
    'backend/tests/FraudDecision.Scenarios',
    'docs/specs/fraud-rules-decision'
)

$patterns = @(
    'BEGIN PRIVATE KEY',
    'BEGIN RSA PRIVATE KEY',
    'BEGIN OPENSSH PRIVATE KEY',
    'github_pat_',
    'ghp_',
    'FRAUD_DECISION_SECRET=',
    'PAYMENT_EXECUTION_SECRET='
)

$findings = @()
foreach ($root in $roots) {
    if (-not (Test-Path $root)) { continue }

    Get-ChildItem $root -Recurse -File |
        Where-Object { $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' } |
        ForEach-Object {
            foreach ($pattern in $patterns) {
                $matches = Select-String -Path $_.FullName -Pattern $pattern -SimpleMatch -ErrorAction SilentlyContinue
                if ($matches) { $findings += "$($_.FullName) -> $pattern" }
            }
        }
}

if ($findings.Count -gt 0) {
    Write-Host 'Fraud decision secret scan FAIL'
    $findings | ForEach-Object { Write-Host $_ }
    exit 1
}

Write-Host 'Fraud decision secret scan PASS'