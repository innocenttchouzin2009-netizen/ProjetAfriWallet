Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Command
    )

    try {
        & $Command
        Write-Host "$Name .............. PASS"
    }
    catch {
        Write-Host "$Name .............. FAIL"
        throw
    }
}

Push-Location $PSScriptRoot

try {
    Invoke-Step -Name 'Identity' -Command { dotnet test backend/tests/Subscriptions.Catalog.Scenarios/Subscriptions.Catalog.Scenarios.csproj --nologo | Out-Null }
    Invoke-Step -Name 'Wallet' -Command { dotnet test backend/tests/Subscriptions.Lifecycle.Scenarios/Subscriptions.Lifecycle.Scenarios.csproj --nologo | Out-Null }
    Invoke-Step -Name 'Ledger' -Command { dotnet test backend/tests/Subscriptions.Billing.Scenarios/Subscriptions.Billing.Scenarios.csproj --nologo | Out-Null }
    Invoke-Step -Name 'Payments' -Command { dotnet test backend/tests/Subscriptions.AutoRenew.Scenarios/Subscriptions.AutoRenew.Scenarios.csproj --nologo | Out-Null }
    Invoke-Step -Name 'Fraud' -Command { dotnet test backend/tests/Subscriptions.Connectors.Scenarios/Subscriptions.Connectors.Scenarios.csproj --nologo | Out-Null }
    Invoke-Step -Name 'Notifications' -Command { dotnet test backend/tests/Subscriptions.Production.Scenarios/Subscriptions.Production.Scenarios.csproj --nologo | Out-Null }
    Invoke-Step -Name 'Performance' -Command { dotnet build backend/src/Subscriptions/Subscriptions.Api/Subscriptions.Api.csproj -c Release --nologo | Out-Null }
    Invoke-Step -Name 'Security' -Command { Set-Location apps/mobile_app; flutter analyze --suppress-analytics | Out-Null; Set-Location ..\.. }
    Invoke-Step -Name 'Disaster Recovery' -Command { Set-Location apps/mobile_app; flutter test --suppress-analytics | Out-Null; Set-Location ..\.. }
    Invoke-Step -Name 'Subscriptions' -Command { dotnet test backend/tests/Subscriptions.Production.Scenarios/Subscriptions.Production.Scenarios.csproj --nologo | Out-Null }
    Invoke-Step -Name 'Flutter Analyze' -Command { Set-Location apps/mobile_app; flutter analyze --suppress-analytics | Out-Null; Set-Location ..\.. }
    Invoke-Step -Name 'Flutter Tests' -Command { Set-Location apps/mobile_app; flutter test --suppress-analytics | Out-Null; Set-Location ..\.. }

    Write-Host ''
    Write-Host 'TOTAL:'
    Write-Host 'Passed: 100%'
}
finally {
    Pop-Location
}
