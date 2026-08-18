$ErrorActionPreference = 'Stop'

$roots = @(
    'backend/src/Merchants',
    'backend/tests/MerchantRegistry.Scenarios',
    'docs/specs/merchant-registry'
)
$patterns = @(
    'BEGIN PRIVATE KEY',
    'BEGIN RSA PRIVATE KEY',
    'BEGIN OPENSSH PRIVATE KEY',
    'github_pat_',
    'ghp_',
    'KYB_PROVIDER_SECRET=',
    'PAYMENT_CAPTURE_SECRET=',
    'SETTLEMENT_SECRET=',
    'PAYOUT_SECRET=',
    'LEDGER_WRITE_SECRET='
)
$findings = @()
foreach ($root in $roots) {
    if (-not (Test-Path $root)) { continue }
    Get-ChildItem $root -Recurse -File |
        Where-Object { $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' } |
        ForEach-Object {
            foreach ($pattern in $patterns) {
                $hits = Select-String -Path $_.FullName -Pattern $pattern -SimpleMatch -ErrorAction SilentlyContinue
                if ($hits) {
                    $findings += "$($_.FullName) -> $pattern"
                }
            }
        }
}
if ($findings.Count -gt 0) {
    $findings | ForEach-Object { Write-Host $_ }
    throw 'Merchant Registry secret scan failed.'
}
Write-Host 'Merchant Registry secret scan PASS'
