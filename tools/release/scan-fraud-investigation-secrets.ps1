$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Fraud/FraudInvestigation.Domain',
    'backend/src/Fraud/FraudInvestigation.Application',
    'backend/src/Fraud/FraudInvestigation.Infrastructure',
    'backend/src/Fraud/FraudInvestigation.Api',
    'backend/tests/FraudInvestigation.Scenarios',
    'docs/specs/fraud-investigation'
)
$patterns = @(
    'BEGIN PRIVATE KEY', 'BEGIN RSA PRIVATE KEY', 'BEGIN OPENSSH PRIVATE KEY',
    'github_pat_', 'ghp_', 'FRAUD_INVESTIGATION_SECRET=',
    'ACCOUNT_RESTRICTION_SECRET=', 'DEVICE_REVOCATION_SECRET='
)
$findings = @()
foreach ($root in $roots) {
    if (-not (Test-Path $root)) { continue }
    Get-ChildItem $root -Recurse -File |
        Where-Object { $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' } |
        ForEach-Object {
            foreach ($pattern in $patterns) {
                if (Select-String -Path $_.FullName -Pattern $pattern -SimpleMatch -ErrorAction SilentlyContinue) {
                    $findings += "$($_.FullName) -> $pattern"
                }
            }
        }
}
if ($findings.Count -gt 0) {
    Write-Host 'Fraud investigation secret scan FAIL'
    $findings | ForEach-Object { Write-Host $_ }
    exit 1
}
Write-Host 'Fraud investigation secret scan PASS'