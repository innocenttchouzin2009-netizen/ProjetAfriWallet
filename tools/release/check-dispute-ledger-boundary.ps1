$ErrorActionPreference = 'Stop'

$forbidden = @(
    'PostLedgerEntryAsync(',
    'AppendLedgerEntryAsync(',
    'ReverseLedgerEntryAsync(',
    'WriteLedgerAsync(',
    'ILedgerWriter',
    'IUniversalLedgerWriter'
)

$files = Get-ChildItem backend/src/Disputes -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' }

foreach ($token in $forbidden) {
    $hits = $files | Select-String -Pattern $token -SimpleMatch -ErrorAction SilentlyContinue
    if ($hits) {
        throw "Direct ledger dependency detected: $token"
    }
}

Write-Host 'Dispute ledger boundary PASS'
