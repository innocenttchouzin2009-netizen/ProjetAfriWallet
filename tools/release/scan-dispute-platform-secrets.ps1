$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Disputes',
    'backend/tests/DisputeReadiness.Scenarios'
)
$patterns = @(
    'BEGIN PRIVATE KEY',
    'BEGIN RSA PRIVATE KEY',
    'BEGIN OPENSSH PRIVATE KEY',
    'github_pat_',
    'ghp_',
    'REAL_REFUND_SECRET=',
    'REAL_CHARGEBACK_SECRET=',
    'LEDGER_WRITE_SECRET=',
    'MERCHANT_BLOCKING_SECRET=',
    'CUSTOMER_SUSPENSION_SECRET='
)
$findings = @()
foreach ($root in $roots) {
    if (-not (Test-Path $root)) {
        throw "Required path missing: $root"
    }
    Get-ChildItem $root -Recurse -File |
        Where-Object { $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' } |
        ForEach-Object {
            foreach ($pattern in $patterns) {
                $match = Select-String -Path $_.FullName -Pattern $pattern -SimpleMatch -ErrorAction SilentlyContinue
                if ($match) {
                    $findings += "$($_.FullName) -> $pattern"
                }
            }
        }
}
if ($findings.Count -gt 0) {
    $findings | ForEach-Object { Write-Host $_ }
    throw 'Dispute platform secret scan failed.'
}
Write-Host 'Dispute platform secret scan PASS'
