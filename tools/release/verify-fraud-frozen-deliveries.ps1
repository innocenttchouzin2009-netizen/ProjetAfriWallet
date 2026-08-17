$ErrorActionPreference = 'Stop'
git fetch origin --prune --tags
$tags = 1..6 | ForEach-Object { "sprint17-dlv-0017.$_" }
foreach ($tag in $tags) {
    Write-Host "`nChecking $tag"
    $localSha = (git rev-list -n 1 "$tag^{}").Trim()
    if (-not $localSha) { throw "$tag local SHA unresolved." }
    $remote = git ls-remote --tags origin "refs/tags/$tag^{}"
    if (-not $remote) { throw "$tag remote tag missing." }
    $remoteSha = ($remote -split '\s+')[0].Trim()
    if ($localSha -ne $remoteSha) { throw "$tag parity failure." }
    git merge-base --is-ancestor $localSha origin/main
    if ($LASTEXITCODE -ne 0) { throw "$tag is not in origin/main." }
    Write-Host "$tag VERIFIED"
}
Write-Host "`nFraud frozen deliveries: 6/6 VERIFIED"