$ErrorActionPreference = 'Stop'
$roots = @(
    'backend/src/Fraud/FraudIntelligence.Domain',
    'backend/src/Fraud/FraudIntelligence.Application',
    'backend/src/Fraud/FraudIntelligence.Infrastructure',
    'backend/src/Fraud/FraudIntelligence.Api',
    'backend/tests/FraudIntelligence.Scenarios',
    'docs/specs/fraud-intelligence'
)
$patterns = @('BEGIN PRIVATE KEY', 'BEGIN RSA PRIVATE KEY', 'BEGIN OPENSSH PRIVATE KEY', 'github_pat_', 'ghp_', 'FRAUD_INTELLIGENCE_SECRET=', 'FRAUD_ENFORCEMENT_SECRET=')
$findings = @()
foreach ($root in $roots) {
    if (-not (Test-Path $root)) { continue }
    Get-ChildItem $root -Recurse -File |
        Where-Object { $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' } |
        ForEach-Object {
            foreach ($pattern in $patterns) {
                if (Select-String -Path $_.FullName -Pattern $pattern -SimpleMatch -ErrorAction SilentlyContinue) { $findings += "$($_.FullName) -> $pattern" }
            }
        }
}
if ($findings.Count -gt 0) {
    Write-Host 'Fraud intelligence secret scan FAIL'
    $findings | ForEach-Object { Write-Host $_ }
    exit 1
}
Write-Host 'Fraud intelligence secret scan PASS'