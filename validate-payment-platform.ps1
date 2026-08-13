[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = $PSScriptRoot
$evidenceDirectory = Join-Path $repositoryRoot "build\payment-readiness-evidence"
$env:AFW_PAYMENT_READINESS_EVIDENCE = $evidenceDirectory

Set-Location $repositoryRoot

if (Test-Path -LiteralPath $evidenceDirectory) {
    Remove-Item -LiteralPath $evidenceDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $evidenceDirectory -Force | Out-Null

Write-Host ""
Write-Host "AFW-DLV-0014.7 - Payment Platform Production Readiness"
Write-Host ""

function Write-Evidence {
    param(
        [Parameter(Mandatory)]
        [string]$FileName,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string[]]$Lines
    )

    $path = Join-Path $evidenceDirectory $FileName
    [IO.File]::WriteAllLines(
        $path,
        $Lines,
        [Text.UTF8Encoding]::new($false))
}

function Invoke-DotNetStep {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$EvidenceFile,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    Write-Host ""
    Write-Host "==> $Name"

    $output = @(& dotnet @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }

    $status = if ($exitCode -eq 0) { "PASS" } else { "FAIL" }
    $lines = @(
        "STEP_NAME=$Name",
        "EXIT_CODE=$exitCode",
        "STEP_STATUS=$status"
    ) + @($output | ForEach-Object { $_.ToString() })

    Write-Evidence -FileName $EvidenceFile -Lines $lines

    if ($exitCode -ne 0) {
        throw "$Name failed with exit code $exitCode"
    }

    Write-Host "$Name PASS"
}

function Invoke-ScriptStep {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$EvidenceFile,

        [Parameter(Mandatory)]
        [string]$ScriptPath
    )

    Write-Host ""
    Write-Host "==> $Name"

    $output = @(& $ScriptPath 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }

    $status = if ($exitCode -eq 0) { "PASS" } else { "FAIL" }
    $lines = @(
        "STEP_NAME=$Name",
        "EXIT_CODE=$exitCode",
        "STEP_STATUS=$status"
    ) + @($output | ForEach-Object { $_.ToString() })

    Write-Evidence -FileName $EvidenceFile -Lines $lines

    if ($exitCode -ne 0) {
        throw "$Name failed with exit code $exitCode"
    }

    Write-Host "$Name PASS"
}

$scenarioProjects = @(
    @{ Name = "Payment Intent Scenarios"; Slug = "payment-intent"; Path = "backend/tests/PaymentIntent.Scenarios/PaymentIntent.Scenarios.csproj" },
    @{ Name = "Payment Routing Scenarios"; Slug = "payment-routing"; Path = "backend/tests/PaymentRouting.Scenarios/PaymentRouting.Scenarios.csproj" },
    @{ Name = "Merchant Acquiring Scenarios"; Slug = "merchant-acquiring"; Path = "backend/tests/MerchantAcquiring.Scenarios/MerchantAcquiring.Scenarios.csproj" },
    @{ Name = "Merchant Settlement Scenarios"; Slug = "merchant-settlement"; Path = "backend/tests/MerchantSettlement.Scenarios/MerchantSettlement.Scenarios.csproj" },
    @{ Name = "Mobile Money Scenarios"; Slug = "mobile-money"; Path = "backend/tests/MobileMoney.Scenarios/MobileMoney.Scenarios.csproj" },
    @{ Name = "Provider Integration Scenarios"; Slug = "provider-integration"; Path = "backend/tests/ProviderIntegration.Scenarios/ProviderIntegration.Scenarios.csproj" }
)

foreach ($scenario in $scenarioProjects) {
    Invoke-DotNetStep `
        -Name $scenario.Name `
        -EvidenceFile "scenario-$($scenario.Slug).log" `
        -Arguments @(
            "run",
            "--project",
            $scenario.Path,
            "-c",
            $Configuration
        )
}

$buildProjects = @(
    @{ Name = "Payment Intent Release Build"; Slug = "payment-intent"; Path = "backend/src/PaymentPlatform/PaymentIntent/PaymentIntent.Api/PaymentIntent.Api.csproj" },
    @{ Name = "Payment Routing Release Build"; Slug = "payment-routing"; Path = "backend/src/PaymentPlatform/PaymentRouting/PaymentRouting.Api/PaymentRouting.Api.csproj" },
    @{ Name = "Merchant Acquiring Release Build"; Slug = "merchant-acquiring"; Path = "backend/src/PaymentPlatform/MerchantAcquiring/MerchantAcquiring.Api/MerchantAcquiring.Api.csproj" },
    @{ Name = "Merchant Settlement Release Build"; Slug = "merchant-settlement"; Path = "backend/src/PaymentPlatform/MerchantSettlement/MerchantSettlement.Api/MerchantSettlement.Api.csproj" },
    @{ Name = "Mobile Money Release Build"; Slug = "mobile-money"; Path = "backend/src/PaymentPlatform/MobileMoney/MobileMoney.Api/MobileMoney.Api.csproj" },
    @{ Name = "Provider Integration Release Build"; Slug = "provider-integration"; Path = "backend/src/PaymentPlatform/ProviderIntegration/ProviderIntegration.Api/ProviderIntegration.Api.csproj" },
    @{ Name = "Payment Readiness Release Build"; Slug = "payment-readiness"; Path = "backend/src/PaymentPlatform/PaymentProductionReadiness/PaymentProductionReadiness.csproj" }
)

foreach ($build in $buildProjects) {
    Invoke-DotNetStep `
        -Name $build.Name `
        -EvidenceFile "build-$($build.Slug).log" `
        -Arguments @(
            "build",
            $build.Path,
            "-c",
            $Configuration,
            "-nologo"
        )
}

Invoke-ScriptStep `
    -Name "Secret Scan" `
    -EvidenceFile "secret-scan.log" `
    -ScriptPath (Join-Path $repositoryRoot "scan-payment-secrets.ps1")

Write-Host ""
Write-Host "==> Dependency Vulnerability Scan"

$dependencyOutput = @()
$vulnerabilities = @()

foreach ($build in $buildProjects) {
    $output = @(& dotnet list $build.Path package --vulnerable --include-transitive 2>&1)
    $exitCode = $LASTEXITCODE
    $dependencyOutput += "PROJECT=$($build.Path)"
    $dependencyOutput += @($output | ForEach-Object { $_.ToString() })
    $output | ForEach-Object { Write-Host $_ }

    if ($exitCode -ne 0) {
        throw "Dependency scan command failed for $($build.Path)."
    }

    $vulnerabilities += @($output | Where-Object {
        $_.ToString() -match 'https://github\.com/advisories/' -or
        $_.ToString() -match '\bNU190[1-4]\b'
    })
}

if ($vulnerabilities.Count -gt 0) {
    Write-Evidence -FileName "dependency-scan.log" -Lines @(
        "STEP_STATUS=FAIL"
    ) + $dependencyOutput
    throw "Vulnerable package detected."
}

$dependencyOutput += "Dependency Vulnerability Scan PASS"
$dependencyOutput += "STEP_STATUS=PASS"
Write-Evidence -FileName "dependency-scan.log" -Lines $dependencyOutput
Write-Host "Dependency Vulnerability Scan PASS"

Invoke-DotNetStep `
    -Name "Payment Production Readiness" `
    -EvidenceFile "readiness-program.log" `
    -Arguments @(
        "run",
        "--project",
        "backend/src/PaymentPlatform/PaymentProductionReadiness/PaymentProductionReadiness.csproj",
        "-c",
        $Configuration,
        "--no-build"
    )

Invoke-DotNetStep `
    -Name "Readiness Scenario Runner" `
    -EvidenceFile "readiness-scenarios.log" `
    -Arguments @(
        "run",
        "--project",
        "backend/tests/PaymentProductionReadiness.Scenarios/PaymentProductionReadiness.Scenarios.csproj",
        "-c",
        $Configuration
    )

Write-Host ""
Write-Host "All AFW-DLV-0014.7 payment readiness validations passed."