$ErrorActionPreference = 'Stop'

$root = 'release/dispute-platform/v1.8.0-rc1'
if (-not (Test-Path $root)) {
    throw 'Dispute RC directory missing.'
}

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

Write-Host 'Dispute RC SHA-256 manifest GENERATED'
