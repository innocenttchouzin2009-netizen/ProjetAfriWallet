$ErrorActionPreference = 'Stop'
$roots = @(
    'backend/src/Fraud/TransactionFraud.Domain',
    'backend/src/Fraud/TransactionFraud.Application',
    'backend/src/Fraud/TransactionFraud.Infrastructure',
    'backend/src/Fraud/TransactionFraud.Api',
    'backend/tests/TransactionFraud.Scenarios',
    'docs/specs/transaction-fraud-detection')
$patterns = @(
    'BEGIN PRIVATE KEY',
    'BEGIN RSA PRIVATE KEY',
    'BEGIN OPENSSH PRIVATE KEY',
    'github_pat_',
    'ghp_',
    'FRAUD_API_KEY=',
    'FRAUD_PROVIDER_SECRET=',
    'FRAUD_DECISION_SECRET=')
$findings = @()
foreach ($root in $roots) {
    if (-not (Test-Path $root)) { continue }
    Get-ChildItem $root -Recurse -File |
        Where-Object { $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' } |
        ForEach-Object {
            foreach ($pattern in $patterns) {
                $match = Select-String -Path $_.FullName -Pattern $pattern -SimpleMatch -ErrorAction SilentlyContinue
                if ($match) { $findings += "$($_.FullName) -> $pattern" }
            }
        }
}
if ($findings.Count -gt 0) {
    $findings | ForEach-Object { Write-Host $_ }
    throw 'Transaction fraud secret scan failed.'
}
Write-Host 'Transaction fraud secret scan PASS'
