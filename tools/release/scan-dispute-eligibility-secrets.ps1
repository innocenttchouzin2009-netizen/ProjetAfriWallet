$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Disputes/DisputeEligibility.Domain',
    'backend/src/Disputes/DisputeEligibility.Application',
    'backend/src/Disputes/DisputeEligibility.Infrastructure',
    'backend/src/Disputes/DisputeEligibility.Api',
    'backend/tests/DisputeEligibility.Scenarios',
    'docs/specs/dispute-eligibility'
)
$patterns = @(
    'BEGIN PRIVATE KEY',
    'BEGIN RSA PRIVATE KEY',
    'BEGIN OPENSSH PRIVATE KEY',
    'github_pat_',
    'ghp_',
    'DISPUTE_PROVIDER_SECRET=',
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
    Write-Host 'Dispute eligibility secret scan FAIL'
    $findings | ForEach-Object { Write-Host $_ }
    exit 1
}
Write-Host 'Dispute eligibility secret scan PASS'
