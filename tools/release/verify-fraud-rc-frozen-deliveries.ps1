$ErrorActionPreference = 'Stop'
git fetch origin --prune --tags
git fetch origin main
$tags = 1..7 | ForEach-Object { "sprint17-dlv-0017.$_" }
foreach ($tag in $tags) {
    Write-Host "`nVerifying $tag"
    $localSha = (git rev-list -n 1 "$tag^{}").Trim()
    if (-not $localSha) { throw "$tag local SHA unresolved." }
    $remote = git ls-remote --tags origin "refs/tags/$tag^{}"
    if (-not $remote) { throw "$tag remote tag missing." }
    $remoteSha = ($remote -split '\s+')[0].Trim()
    if ($localSha -ne $remoteSha) { throw "$tag SHA PARITY FAILED." }
    git merge-base --is-ancestor $localSha origin/main
    if ($LASTEXITCODE -ne 0) { throw "$tag is not part of origin/main." }
    Write-Host "LOCAL : $localSha"
    Write-Host "REMOTE: $remoteSha"
    Write-Host 'PARITY: VERIFIED'
}
Write-Host "`nFraud frozen deliveries: 7/7 VERIFIED"