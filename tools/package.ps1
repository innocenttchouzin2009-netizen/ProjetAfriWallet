param(
    [string]$Version = "0.4.8.7"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path "artifacts" | Out-Null
New-Item -ItemType Directory -Force -Path "artifacts/openapi" | Out-Null
New-Item -ItemType Directory -Force -Path "artifacts/sbom" | Out-Null
New-Item -ItemType Directory -Force -Path "artifacts/release-notes" | Out-Null

Copy-Item -Force -Recurse "docs/api/*" "artifacts/openapi/"

@"
# AfriWallet Release $Version

- Enterprise CI/CD workflow scaffolded
- Build, test, security, performance, and release automation introduced
- ADRs and runbooks added for deployment and supply chain controls
"@ | Set-Content -Path "artifacts/release-notes/release-notes-$Version.md"

Write-Host "Packaging complete"
