param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$root = Split-Path -Parent $root
$scenarioProject = Join-Path $root 'backend/tests/ComplianceProfile.Scenarios/ComplianceProfile.Scenarios.csproj'
$apiProject = Join-Path $root 'backend/src/CompliancePlatform/ComplianceProfile.Api/ComplianceProfile.Api.csproj'

Write-Host '== Compliance profile release validation =='

dotnet restore $scenarioProject

dotnet build $scenarioProject --configuration $Configuration --no-restore

dotnet run --project $scenarioProject --configuration $Configuration --no-build

dotnet build $apiProject --configuration $Configuration --no-restore

Write-Host 'Compliance profile release validation passed.'
