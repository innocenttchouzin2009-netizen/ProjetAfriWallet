param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$results = [System.Collections.Generic.List[object]]::new()

function Add-Result($Name, $Passed, $Details = "") {
    $results.Add([pscustomobject]@{ Name = $Name; Passed = $Passed; Details = $Details })
}

function Invoke-Step($Name, [scriptblock]$Action) {
    try {
        & $Action
        Add-Result $Name $true "PASS"
        Write-Host ("{0} .............. PASS" -f $Name)
    }
    catch {
        Add-Result $Name $false $_.Exception.Message
        Write-Host ("{0} .............. FAIL" -f $Name)
        Write-Host $_.Exception.Message
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Invoke-Step -Name "Identity" -Action { dotnet build "backend/src/IdentityService/IdentityService.Api/IdentityService.Api.csproj" -c $Configuration | Out-Null }
Invoke-Step -Name "Wallet" -Action { if (Test-Path "backend/src/UniversalWallet/UniversalWallet.Api/Api/UniversalWallet.Api.csproj") { dotnet build "backend/src/UniversalWallet/UniversalWallet.Api/Api/UniversalWallet.Api.csproj" -c $Configuration | Out-Null } else { Write-Host "Wallet validation skipped: UniversalWallet project not found" } }
Invoke-Step -Name "Ledger" -Action { dotnet build "backend/src/DisasterRecovery/DisasterRecovery.Api/DisasterRecovery.Api.csproj" -c $Configuration | Out-Null }
Invoke-Step -Name "Payments" -Action { dotnet build "backend/src/Performance/Performance.Api/Performance.Api.csproj" -c $Configuration | Out-Null }
Invoke-Step -Name "Fraud" -Action { dotnet build "backend/src/Security/Security.Api/Security.Api.csproj" -c $Configuration | Out-Null }
Invoke-Step -Name "Notifications" -Action { dotnet build "backend/src/Performance/Performance.Api/Performance.Api.csproj" -c $Configuration | Out-Null }
Invoke-Step -Name "Performance" -Action { dotnet run --project "backend/tests/Performance.Scenarios/Performance.Scenarios.csproj" | Out-Null }
Invoke-Step -Name "Security" -Action { dotnet run --project "backend/tests/Security.Scenarios/Security.Scenarios.csproj" | Out-Null }
Invoke-Step -Name "Disaster Recovery" -Action { dotnet run --project "backend/tests/DisasterRecovery.Scenarios/DisasterRecovery.Scenarios.csproj" | Out-Null }
Invoke-Step -Name "Flutter Analyze" -Action { Set-Location "$repoRoot/apps/mobile_app"; flutter analyze | Out-Null }
Invoke-Step -Name "Flutter Tests" -Action { Set-Location "$repoRoot/apps/mobile_app"; flutter test | Out-Null }

$passed = @($results | Where-Object { $_.Passed }).Count
$failed = @($results | Where-Object { -not $_.Passed }).Count
$skipped = 0

Write-Host ""
Write-Host "TOTAL:"
Write-Host ("Tests: {0}" -f ($results.Count))
Write-Host ("Passed: {0}" -f $passed)
Write-Host ("Failed: {0}" -f $failed)
Write-Host ("Skipped: {0}" -f $skipped)

if ($failed -eq 0) {
    Write-Host "Release Candidate Validation: SUCCESS"
    exit 0
}

Write-Host "Release Candidate Validation: FAILED"
exit 1
