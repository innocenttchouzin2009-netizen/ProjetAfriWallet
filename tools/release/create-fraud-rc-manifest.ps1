$ErrorActionPreference = 'Stop'
$root = 'release/fraud-platform/v1.7.0-rc1'
if (-not (Test-Path $root)) { throw 'Fraud RC package directory missing.' }
$manifest = Join-Path $root 'manifest.sha256'
$resolvedRoot = (Resolve-Path $root).Path
Get-ChildItem $root -Recurse -File |
    Where-Object { $_.FullName -ne [System.IO.Path]::GetFullPath($manifest) } |
    Sort-Object FullName |
    ForEach-Object {
        $hash = Get-FileHash $_.FullName -Algorithm SHA256
        $relative = $_.FullName.Substring($resolvedRoot.Length).TrimStart('\', '/').Replace('\', '/')
        "$($hash.Hash.ToLowerInvariant())  $relative"
    } | Set-Content $manifest
Write-Host 'Fraud RC SHA-256 manifest GENERATED'