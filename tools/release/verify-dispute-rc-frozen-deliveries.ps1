$ErrorActionPreference = 'Stop'

git fetch origin --prune --tags
git fetch origin main

$tags = @(
    'sprint18-dlv-0018.1',
    'sprint18-dlv-0018.2',
    'sprint18-dlv-0018.3',
    'sprint18-dlv-0018.4',
    'sprint18-dlv-0018.5',
    'sprint18-dlv-0018.6',
    'sprint18-dlv-0018.7'
)

foreach ($tag in $tags) {
    Write-Host ""
    Write-Host "Verifying $tag"

    $localSha = (git rev-list -n 1 "$tag^{}").Trim()
    if (-not $localSha) {
        throw "$tag local SHA unresolved."
    }

    $remote = git ls-remote --tags origin "refs/tags/$tag^{}"
    if ($remote) {
        $remoteSha = ($remote -split "\s+")[0].Trim()
    } else {
        $remote = git ls-remote --tags origin "refs/tags/$tag"
        if (-not $remote) {
            throw "$tag remote tag missing."
        }
        $remoteSha = ($remote -split "\s+")[0].Trim()
    }

    if ($localSha -ne $remoteSha) {
        throw "$tag SHA PARITY FAILED."
    }

    git merge-base --is-ancestor $localSha origin/main
    if ($LASTEXITCODE -ne 0) {
        throw "$tag not contained in origin/main."
    }

    Write-Host "LOCAL : $localSha"
    Write-Host "REMOTE: $remoteSha"
    Write-Host "PARITY: VERIFIED"
}

Write-Host ""
Write-Host "Dispute frozen deliveries: 7/7 VERIFIED"
