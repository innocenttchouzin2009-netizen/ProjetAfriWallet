$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Disputes/DisputeIntelligence',
    'backend/tests/DisputeIntelligence.Scenarios',
    'docs/specs/dispute-intelligence'
)
$patterns = @(
    'BEGIN PRIVATE KEY',
    'BEGIN RSA PRIVATE KEY',
    'BEGIN OPENSSH PRIVATE KEY',
    'github_pat_',
    'ghp_',
    'MERCHANT_BLOCKING_SECRET=',
    'CUSTOMER_SUSPENSION_SECRET=',
    'REFUND_EXECUTION_SECRET=',
    'LEDGER_WRITE_SECRET='
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
    $findings | ForEach-Object { Write-Host $_ }
    throw 'Dispute intelligence secret scan failed.'
}
Write-Host 'Dispute intelligence secret scan PASS'
