$ErrorActionPreference = 'Stop'
$forbidden = @('Microsoft.ML', 'TensorFlow', 'ONNXRuntime', 'PredictionEngine')
$files = Get-ChildItem backend/src/Fraud -Recurse -File | Where-Object { $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' }
foreach ($token in $forbidden) { if ($files | Select-String -Pattern $token -SimpleMatch -ErrorAction SilentlyContinue) { throw "Opaque ML dependency detected: $token" } }
Write-Host 'Fraud deterministic/ML boundary PASS'