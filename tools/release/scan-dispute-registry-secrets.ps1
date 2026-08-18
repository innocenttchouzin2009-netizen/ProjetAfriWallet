$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Disputes/DisputeRegistry.Domain',
    'backend/src/Disputes/DisputeRegistry.Application',
    'backend/src/Disputes/DisputeRegistry.Infrastructure',
    'backend/src/Disputes/DisputeRegistry.Api',
    'backend/tests/DisputeRegistry.Scenarios',
    'docs/specs/dispute-registry'
)
$patterns = @(
    'BEGIN PRIVATE KEY',
    'BEGIN RSA PRIVATE KEY',
    'BEGIN OPENSSH PRIVATE KEY',
    'github_pat_',
    'ghp_',
    'DISPUTE_REGISTRY_SECRET=',
    'REFUND_EXECUTION_SECRET=',
    'CHARGEBACK_SECRET='
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
    Write-Host 'Dispute registry secret scan FAIL'
    $findings | ForEach-Object { Write-Host $_ }
    exit 1
}
Write-Host 'Dispute registry secret scan PASS'
