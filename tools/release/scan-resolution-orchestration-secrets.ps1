$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Disputes/ResolutionOrchestration',
    'backend/tests/ResolutionOrchestration.Scenarios',
    'docs/specs/resolution-orchestration'
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
    'PROVIDER_SETTLEMENT_SECRET='
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
    throw 'Resolution orchestration secret scan failed.'
}
Write-Host 'Resolution orchestration secret scan PASS'
