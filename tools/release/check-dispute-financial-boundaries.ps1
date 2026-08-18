$ErrorActionPreference = 'Stop'

$forbidden = @(
    'ExecuteRealRefundAsync(',
    'SubmitRealChargebackAsync(',
    'MoveMoneyAsync(',
    'DebitWalletAsync(',
    'CreditWalletAsync(',
    'ExecuteSettlementAsync('
)

$files = Get-ChildItem backend/src/Disputes -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' }

foreach ($token in $forbidden) {
    $hits = $files | Select-String -Pattern $token -SimpleMatch -ErrorAction SilentlyContinue
    if ($hits) {
        throw "Forbidden financial execution dependency: $token"
    }
}

Write-Host 'Dispute financial execution boundary PASS'
