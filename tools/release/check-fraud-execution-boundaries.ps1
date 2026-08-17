$ErrorActionPreference = 'Stop'
$forbidden = @('ExecutePaymentAsync(', 'SuspendAccountAsync(', 'FreezeWalletAsync(', 'RevokeDeviceAsync(', 'CancelBankTransferAsync(')
$files = Get-ChildItem backend/src/Fraud -Recurse -File | Where-Object { $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' }
foreach ($token in $forbidden) { if ($files | Select-String -Pattern $token -SimpleMatch -ErrorAction SilentlyContinue) { throw "Forbidden fraud enforcement dependency: $token" } }
Write-Host 'Fraud execution boundary PASS'