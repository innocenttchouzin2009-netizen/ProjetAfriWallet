param(
    [string]$Version = "0.4.8.7"
)

$ErrorActionPreference = "Stop"

Write-Host "Creating release for version $Version"
& "$PSScriptRoot/package.ps1" -Version $Version
