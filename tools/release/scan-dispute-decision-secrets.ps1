$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Disputes/DisputeDecision',
    'backend/tests/DisputeDecision.Scenarios',
    'docs/specs/dispute-decision'
)
$patterns = @(
    'BEGIN PRIVATE KEY',
    'BEGIN RSA PRIVATE KEY',
    'BEGIN OPENSSH PRIVATE KEY',
    'github_pat_',
    'ghp_',
    'REFUND_EXECUTION_SECRET=',
    'CHARGEBACK_EXECUTION_SECRET=',
    'LEDGER_WRITE_SECRET=',
    'PAYMENT_EXECUTION_SECRET='
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
    throw 'Dispute decision secret scan failed.'
}
Write-Host 'Dispute decision secret scan PASS'
