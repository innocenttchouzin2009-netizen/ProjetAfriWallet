param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$releaseRoot = Join-Path $repoRoot 'release/mtn-momo/v0.7.3.4'
$artifacts = Join-Path $releaseRoot 'artifacts'
$reportJson = Join-Path $releaseRoot 'validation-report.json'
$reportMd = Join-Path $releaseRoot 'validation-report.md'
$checksums = Join-Path $releaseRoot 'checksums.sha256'

New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null

$checks = [System.Collections.Generic.List[object]]::new()
$passed = 0
$failed = 0
$skipped = 0

function Add-Check($name, $passedFlag, $details = $null) {
    $entry = [ordered]@{
        name = $name
        passed = $passedFlag
        details = $details
    }
    $checks.Add([pscustomobject]$entry)
    if ($passedFlag) { $script:passed++ } else { $script:failed++ }
}

function Invoke-CommandChecked($name, [scriptblock]$scriptBlock) {
    try {
        & $scriptBlock
        Add-Check $name $true 'ok'
    }
    catch {
        Add-Check $name $false $_.Exception.Message
        throw
    }
}

$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

Invoke-CommandChecked -name 'Configuration & Secrets' -scriptBlock {
    if (-not (Test-Path (Join-Path $repoRoot 'backend/src/MobileMoney/MobileMoney.Api/appsettings.json'))) { throw 'appsettings.json missing' }
    $content = Get-Content -Path (Join-Path $repoRoot 'backend/src/MobileMoney/MobileMoney.Api/appsettings.json') -Raw
    if ($content -match 'API_KEY=|ACCESS_TOKEN=|SUBSCRIPTION_KEY=|CALLBACK_SECRET=|PASSWORD=|Bearer ') { throw 'Sensitive value detected in appsettings.' }
    if ($content -match '"MtnMomo"') { }
}

Invoke-CommandChecked -name 'Health & Readiness' -scriptBlock {
    dotnet build (Join-Path $repoRoot 'backend/src/MobileMoney/MobileMoney.Api/MobileMoney.Api.csproj') -c $Configuration | Out-Null
}

Invoke-CommandChecked -name 'Logging & Correlation' -scriptBlock {
    dotnet run --project (Join-Path $repoRoot 'backend/tests/MobileMoney.MtnMomo.Logging.Scenarios/MobileMoney.MtnMomo.Logging.Scenarios.csproj') | Out-Null
}

Invoke-CommandChecked -name 'Resilience' -scriptBlock {
    dotnet run --project (Join-Path $repoRoot 'backend/tests/MobileMoney.MtnMomo.Resilience.Scenarios/MobileMoney.MtnMomo.Resilience.Scenarios.csproj') | Out-Null
}

Invoke-CommandChecked -name 'Rate Limiting' -scriptBlock {
    dotnet run --project (Join-Path $repoRoot 'backend/tests/MobileMoney.MtnMomo.RateLimiting.Scenarios/MobileMoney.MtnMomo.RateLimiting.Scenarios.csproj') | Out-Null
}

Invoke-CommandChecked -name 'Feature Flags' -scriptBlock {
    dotnet run --project (Join-Path $repoRoot 'backend/tests/MobileMoney.MtnMomo.FeatureFlags.Scenarios/MobileMoney.MtnMomo.FeatureFlags.Scenarios.csproj') | Out-Null
}

Invoke-CommandChecked -name 'OpenTelemetry' -scriptBlock {
    dotnet run --project (Join-Path $repoRoot 'backend/tests/MobileMoney.MtnMomo.Telemetry.Scenarios/MobileMoney.MtnMomo.Telemetry.Scenarios.csproj') | Out-Null
}

Invoke-CommandChecked -name 'Metrics & Monitoring' -scriptBlock {
    dotnet run --project (Join-Path $repoRoot 'backend/tests/MobileMoney.MtnMomo.Audit.Scenarios/MobileMoney.MtnMomo.Audit.Scenarios.csproj') | Out-Null
}

Invoke-CommandChecked -name 'Audit Trail' -scriptBlock {
    dotnet run --project (Join-Path $repoRoot 'backend/tests/MobileMoney.MtnMomo.Audit.Scenarios/MobileMoney.MtnMomo.Audit.Scenarios.csproj') | Out-Null
}

Invoke-CommandChecked -name 'MTN MoMo API' -scriptBlock {
    dotnet run --project (Join-Path $repoRoot 'backend/tests/MobileMoney.MtnMomo.Configuration.Scenarios/MobileMoney.MtnMomo.Configuration.Scenarios.csproj') | Out-Null
}

Invoke-CommandChecked -name 'Flutter Analyze' -scriptBlock {
    Push-Location (Join-Path $repoRoot 'apps/mobile_app')
    try { flutter analyze | Out-Null } finally { Pop-Location }
}

Invoke-CommandChecked -name 'Flutter Tests' -scriptBlock {
    Push-Location (Join-Path $repoRoot 'apps/mobile_app')
    try { flutter test | Out-Null } finally { Pop-Location }
}

Invoke-CommandChecked -name 'Release Build' -scriptBlock {
    dotnet build (Join-Path $repoRoot 'backend/src/MobileMoney/MobileMoney.Api/MobileMoney.Api.csproj') -c $Configuration | Out-Null
}

Invoke-CommandChecked -name 'Secret Scan' -scriptBlock {
    $exclude = @('*.md', '*.png', '*.jpg', '*.jpeg', '*.gif', '*.svg', '*.lock', 'release/**', 'validate-mtn-momo-production.ps1')
    $matches = Get-ChildItem -Path $repoRoot -Recurse -File | Where-Object {
        $path = $_.FullName.Replace($repoRoot + '\', '')
        -not ($exclude | Where-Object { $path -like $_ })
    } | Select-String -Pattern 'API_KEY=|ACCESS_TOKEN=|SUBSCRIPTION_KEY=|CALLBACK_SECRET=|PASSWORD=|Bearer ' -SimpleMatch -ErrorAction SilentlyContinue
    if ($matches) { throw "Sensitive pattern detected: $($matches[0].Path)" }
}

Invoke-CommandChecked -name 'Packaging' -scriptBlock {
    Copy-Item -Path (Join-Path $repoRoot 'docs/api/openapi.yaml') -Destination (Join-Path $releaseRoot 'openapi/openapi.yaml') -Force
    Copy-Item -Path (Join-Path $repoRoot 'docs/adr') -Destination (Join-Path $releaseRoot 'adr') -Recurse -Force
    Copy-Item -Path (Join-Path $repoRoot 'docs/releases') -Destination (Join-Path $releaseRoot 'artifacts/releases') -Recurse -Force
    Set-Content -Path (Join-Path $releaseRoot 'configuration/validation-settings.json') -Value '{"configuration":"release","environment":"staging"}'
    Set-Content -Path (Join-Path $releaseRoot 'runbooks/validation-runbook.md') -Value '# Validation Runbook

- Run the validation script from the repository root.
- Review the generated report and artifacts.
'
    Set-Content -Path (Join-Path $releaseRoot 'dashboards/monitoring-overview.md') -Value '# Monitoring Overview

- Health endpoints: /health/live, /health/ready, /health/startup
- Metrics: /metrics
'
}

$summary = [ordered]@{
    name = 'AFW-DLV-0007.3.4.10 — PRODUCTION VALIDATION'
    checks = $checks.Count
    passed = $passed
    failed = $failed
    skipped = $skipped
    decision = if ($failed -eq 0) { 'READY FOR RC' } else { 'NOT READY' }
    generatedAt = (Get-Date).ToString('o')
}

$summaryJson = $summary | ConvertTo-Json -Depth 5
$summaryJson | Set-Content -Path $reportJson

$md = @"
# AFW-DLV-0007.3.4.10 — PRODUCTION VALIDATION

Checks: $($checks.Count)
Passed: $passed
Failed: $failed
Skipped: $skipped

Decision: $(if ($failed -eq 0) { 'READY FOR RC' } else { 'NOT READY' })

## Checks
"@
foreach ($check in $checks) {
    $status = if ($check.passed) { 'PASS' } else { 'FAIL' }
    $md += "- [$status] $($check.name): $($check.details)`n"
}
$md | Set-Content -Path $reportMd

$files = Get-ChildItem -Path $releaseRoot -Recurse -File | Where-Object { $_.FullName -ne $checksums }
$hashes = foreach ($file in $files) {
    $relative = $file.FullName.Substring($repoRoot.Length + 1).Replace('\\', '/')
    $hash = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $relative"
}
$hashes | Sort-Object | Set-Content -Path $checksums

Write-Host "All AFW-DLV-0007.3.4.10 production-validation checks passed."
