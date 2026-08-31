$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$paths = @("backend/tests/MerchantReadiness.Scenarios", "docs/specs/merchant-readiness", ".github/workflows/merchant-readiness.yml", "tools/release/validate-merchant-readiness.ps1")
$patterns = @('AKIA[0-9A-Z]{16}', '-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----', '(?i)(client_secret|api_key|access_token)\s*[:=]\s*["''][^"'']{12,}["'']')
$hits = @()
foreach ($relative in $paths) {
  $path = Join-Path $root $relative
  if (-not (Test-Path $path)) { continue }
  $items = if ((Get-Item $path).PSIsContainer) { Get-ChildItem $path -Recurse -File } else { @(Get-Item $path) }
  foreach ($item in $items) { foreach ($pattern in $patterns) { if (Select-String -Path $item.FullName -Pattern $pattern -Quiet) { $hits += $item.FullName } } }
}
if ($hits.Count -gt 0) { Write-Error ("Potential secrets detected: " + ($hits -join ', ')); exit 1 }
Write-Host "Merchant readiness secret scan PASS"