$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Disputes/DisputeInvestigation.Domain',
    'backend/src/Disputes/DisputeInvestigation.Application',
    'backend/src/Disputes/DisputeInvestigation.Infrastructure',
    'backend/src/Disputes/DisputeInvestigation.Api',
    'backend/tests/DisputeInvestigation.Scenarios',
    'docs/specs/dispute-investigation'
)
$patterns = @(
    'BEGIN PRIVATE KEY',
    'BEGIN RSA PRIVATE KEY',
    'BEGIN OPENSSH PRIVATE KEY',
    'github_pat_',
    'ghp_',
    'EVIDENCE_STORAGE_SECRET=',
    'CHARGEBACK_SECRET=',
    'REFUND_EXECUTION_SECRET='
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
    Write-Host 'Dispute investigation secret scan FAIL'
    $findings | ForEach-Object { Write-Host $_ }
    exit 1
}
Write-Host 'Dispute investigation secret scan PASS'
