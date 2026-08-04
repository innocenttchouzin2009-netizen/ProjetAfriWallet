param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$testProjects = @(
    "backend/tests/UniversalWallet.Scenarios/UniversalWallet.Scenarios.csproj",
    "backend/tests/UniversalWallet.Balance.Scenarios/UniversalWallet.Balance.Scenarios.csproj",
    "backend/tests/UniversalWallet.Fx.Scenarios/UniversalWallet.Fx.Scenarios.csproj",
    "backend/tests/Security.Scenarios/Security.Scenarios.csproj",
    "backend/tests/Performance.Scenarios/Performance.Scenarios.csproj",
    "backend/tests/DisasterRecovery.Scenarios/DisasterRecovery.Scenarios.csproj"
)

foreach ($project in $testProjects) {
    Write-Host "Running tests for $project"
    dotnet run --project $project
}
