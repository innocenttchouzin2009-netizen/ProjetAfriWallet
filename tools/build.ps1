param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$projects = @(
    "backend/src/IdentityService/IdentityService.Api/IdentityService.Api.csproj",
    "backend/src/UniversalWallet/UniversalWallet.Api/Api/UniversalWallet.Api.csproj",
    "backend/src/Security/Security.Api/Security.Api.csproj",
    "backend/src/Performance/Performance.Api/Performance.Api.csproj",
    "backend/src/DisasterRecovery/DisasterRecovery.Api/DisasterRecovery.Api.csproj"
)

foreach ($project in $projects) {
    if (-not (Test-Path $project)) {
        Write-Host "Skipping missing project $project"
        continue
    }

    Write-Host "Building $project"
    dotnet build $project -c $Configuration
}
