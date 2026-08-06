param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$docsRoot = Join-Path $repoRoot 'docs/specs/risk-platform/afw-dlv-0011.8'
$releaseRoot = Join-Path $repoRoot 'release/risk-platform/v1.1.0-rc1'
$checksumsPath = Join-Path $releaseRoot 'checksums.sha256'
$reportJsonPath = Join-Path $releaseRoot 'validation-report.json'
$reportMdPath = Join-Path $releaseRoot 'validation-report.md'
$manifestPath = Join-Path $releaseRoot 'manifest.json'

$checks = [System.Collections.Generic.List[object]]::new()
$script:passed = 0
$script:failed = 0
$script:skipped = 0

function Add-CheckResult {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Details
    )

    $checks.Add([pscustomobject]@{
        Name = $Name
        Passed = $Passed
        Details = $Details
    })

    if ($Passed) {
        $script:passed++
    }
    else {
        $script:failed++
    }
}

function Invoke-ValidationCheck {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    try {
        & $Action
        Add-CheckResult -Name $Name -Passed $true -Details 'ok'
    }
    catch {
        Add-CheckResult -Name $Name -Passed $false -Details $_.Exception.Message
    }
}

function Assert-True {
    param(
        [object]$Condition,
        [string]$Message
    )

    if (-not [bool]$Condition) {
        throw $Message
    }
}

function Get-FileText {
    param([string]$Path)

    if (-not (Test-Path -Path $Path)) {
        throw "Missing required file: $Path"
    }

    return Get-Content -Path $Path -Raw
}

function Invoke-Dotnet {
    param(
        [string[]]$DotnetParams
    )

    & dotnet @DotnetParams
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed: dotnet $($DotnetParams -join ' ')"
    }
}

function Test-IgnorableSecretFinding {
    param(
        [string]$RelativePath,
        [string]$Line
    )

    if ($RelativePath -match '(?i)\.example$|\.sample$') {
        return $true
    }

    if ($RelativePath -match '(?i)README|CHANGELOG|CONTRIBUTING|SECURITY|\.md$') {
        return $true
    }

    if ($RelativePath -match '(?i)build-risk-release\.ps1$|validate-risk-platform\.ps1$|validate-.*-production\.ps1$') {
        return $true
    }

    if ($RelativePath -match '(?i)\tests\|/tests/') {
        if ($Line -match '(?i)fake|dummy|sample|example|fixture|test') {
            return $true
        }
    }

    if ($Line -match '(?i)fake|dummy|sample|example|placeholder|documentation|env var|variable name') {
        return $true
    }

    if ($Line -match '(?i)Bearer\s+\{.*\}|Bearer\s+\$\(|Bearer\s+<.*>') {
        return $true
    }

    return $false
}

Invoke-ValidationCheck -Name 'Fraud Detection Engine' -Action {
    Invoke-Dotnet -DotnetParams @('run', '--project', (Join-Path $repoRoot 'backend/tests/Fraud.Scenarios/Fraud.Scenarios.csproj'))
}

Invoke-ValidationCheck -Name 'AML Monitoring' -Action {
    Invoke-Dotnet -DotnetParams @('run', '--project', (Join-Path $repoRoot 'backend/tests/AML.Scenarios/AML.Scenarios.csproj'))
}

Invoke-ValidationCheck -Name 'Unified Risk Scoring' -Action {
    Invoke-Dotnet -DotnetParams @('run', '--project', (Join-Path $repoRoot 'backend/tests/RiskScoring.Scenarios/RiskScoring.Scenarios.csproj'))
}

Invoke-ValidationCheck -Name 'Device Intelligence' -Action {
    Invoke-Dotnet -DotnetParams @('run', '--project', (Join-Path $repoRoot 'backend/tests/Device.Scenarios/Device.Scenarios.csproj'))
}

Invoke-ValidationCheck -Name 'Compliance Case Management' -Action {
    Invoke-Dotnet -DotnetParams @('run', '--project', (Join-Path $repoRoot 'backend/tests/Compliance.Scenarios/Compliance.Scenarios.csproj'))
}

Invoke-ValidationCheck -Name 'Regulatory Reporting' -Action {
    Invoke-Dotnet -DotnetParams @('run', '--project', (Join-Path $repoRoot 'backend/tests/RegulatoryReporting.Scenarios/RegulatoryReporting.Scenarios.csproj'))
}

Invoke-ValidationCheck -Name 'Configuration & Secrets' -Action {
    $settingsPath = Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/appsettings.json'
    $settings = (Get-FileText -Path $settingsPath) | ConvertFrom-Json
    Assert-True -Condition (-not $settings.RiskProduction.EnableProductionMode) -Message 'Production mode must be disabled by default.'
    Assert-True -Condition (($settings.RiskProduction.RequiredEnvironmentVariables | Measure-Object).Count -ge 3) -Message 'Required environment variables missing.'
}

Invoke-ValidationCheck -Name 'Health Checks' -Action {
    $programText = Get-FileText -Path (Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/Program.cs')
    foreach ($path in @('/health/live', '/health/ready', '/health/startup')) {
        Assert-True -Condition ($programText.Contains($path)) -Message "Missing health endpoint: $path"
    }
}

Invoke-ValidationCheck -Name 'Logging & Correlation' -Action {
    $programText = Get-FileText -Path (Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/Program.cs')
    Assert-True -Condition ($programText.Contains('X-Correlation-Id')) -Message 'Correlation ID propagation missing.'
}

Invoke-ValidationCheck -Name 'Resilience' -Action {
    $settings = (Get-FileText -Path (Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/appsettings.json')) | ConvertFrom-Json
    Assert-True -Condition ([int]$settings.Resilience.TimeoutSeconds -gt 0) -Message 'Timeout missing.'
    Assert-True -Condition ([int]$settings.Resilience.Retry.MaxRetries -ge 0) -Message 'Retry missing.'
    Assert-True -Condition ([int]$settings.Resilience.CircuitBreaker.BreakDurationSeconds -gt 0) -Message 'Circuit breaker missing.'
}

Invoke-ValidationCheck -Name 'Rate Limiting' -Action {
    $programText = Get-FileText -Path (Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/Program.cs')
    Assert-True -Condition ($programText.Contains('AddRateLimiter')) -Message 'Rate limiting not configured.'
    Assert-True -Condition ($programText.Contains('RequireRateLimiting')) -Message 'Rate limiting not applied.'
}

Invoke-ValidationCheck -Name 'Feature Flags' -Action {
    $settings = (Get-FileText -Path (Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/appsettings.json')) | ConvertFrom-Json
    foreach ($flagName in @('EnableMetricsEndpoint', 'EnableEnhancedAudit', 'EnableStrictSecretScan')) {
        Assert-True -Condition ($null -ne $settings.FeatureFlags.$flagName) -Message "Missing feature flag: $flagName"
    }
}

Invoke-ValidationCheck -Name 'OpenTelemetry' -Action {
    $settings = (Get-FileText -Path (Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/appsettings.json')) | ConvertFrom-Json
    Assert-True -Condition ($settings.OpenTelemetry.Enabled) -Message 'OpenTelemetry must be enabled.'
}

Invoke-ValidationCheck -Name 'Metrics & Monitoring' -Action {
    $programText = Get-FileText -Path (Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/Program.cs')
    Assert-True -Condition ($programText.Contains('/metrics')) -Message '/metrics endpoint missing.'
}

Invoke-ValidationCheck -Name 'Audit Trail' -Action {
    $programText = Get-FileText -Path (Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/Program.cs')
    Assert-True -Condition ($programText.Contains('/internal/audit/events')) -Message 'Audit endpoint missing.'
}

Invoke-ValidationCheck -Name 'Release Build' -Action {
    Invoke-Dotnet -DotnetParams @('build', (Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/RiskPlatform.Production.csproj'), '-c', $Configuration)
}

Invoke-ValidationCheck -Name 'Secret Scan' -Action {
    $patterns = @('API_KEY=', 'ACCESS_TOKEN=', 'CLIENT_SECRET=', 'JWT_SECRET=', 'CALLBACK_SECRET=', 'PASSWORD=', 'Bearer\s+[A-Za-z0-9\-_\.]{20,}')
    $files = Get-ChildItem -Path $repoRoot -Recurse -File | Where-Object { $_.FullName -notmatch '(?i)\\\.git\\|\\bin\\|\\obj\\|\\.vs\\|\\release\\' }
    $findings = [System.Collections.Generic.List[string]]::new()
    foreach ($file in $files) {
        $relativePath = $file.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
        foreach ($pattern in $patterns) {
            $scanResults = Select-String -Path $file.FullName -Pattern $pattern -AllMatches -ErrorAction SilentlyContinue
            foreach ($match in $scanResults) {
                if (-not (Test-IgnorableSecretFinding -RelativePath $relativePath -Line $match.Line)) {
                    $findings.Add(("{0}:{1}: {2}" -f $relativePath, $match.LineNumber, $match.Line))
                }
            }
        }
    }
    if ($findings.Count -gt 0) { throw "Potential real secrets found. First finding: $($findings[0])" }
}

Invoke-ValidationCheck -Name 'Packaging' -Action {
    Remove-Item -Path (Join-Path $releaseRoot 'openapi') -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $releaseRoot 'adr') -Recurse -Force -ErrorAction SilentlyContinue

    foreach ($dir in @(
        $releaseRoot,
        (Join-Path $releaseRoot 'openapi'),
        (Join-Path $releaseRoot 'adr'),
        (Join-Path $releaseRoot 'runbooks'),
        (Join-Path $releaseRoot 'dashboards'),
        (Join-Path $releaseRoot 'configuration'),
        (Join-Path $releaseRoot 'artifacts'),
        (Join-Path $releaseRoot 'rollback')
    )) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    Copy-Item -Path (Join-Path $docsRoot 'PRD-AFW-DLV-0011.8.md') -Destination (Join-Path $releaseRoot 'artifacts/PRD-AFW-DLV-0011.8.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'CHANGELOG.md') -Destination (Join-Path $releaseRoot 'changelog.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'release-notes.md') -Destination (Join-Path $releaseRoot 'release-notes.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'manifest-template.json') -Destination (Join-Path $releaseRoot 'manifest.json') -Force
    Copy-Item -Path (Join-Path $docsRoot 'validation-report-template.md') -Destination (Join-Path $releaseRoot 'validation-report.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'validation-report-template.json') -Destination (Join-Path $releaseRoot 'validation-report.json') -Force
    Copy-Item -Path (Join-Path $docsRoot 'rollback-plan.md') -Destination (Join-Path $releaseRoot 'rollback/rollback-plan.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'administration-guide.md') -Destination (Join-Path $releaseRoot 'runbooks/administration-guide.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'operations-guide.md') -Destination (Join-Path $releaseRoot 'runbooks/operations-guide.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'qa-checklist.md') -Destination (Join-Path $releaseRoot 'artifacts/qa-checklist.md') -Force
    Get-ChildItem -Path (Join-Path $docsRoot 'openapi') -File | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination (Join-Path $releaseRoot ("openapi/{0}" -f $_.Name)) -Force
    }
    Get-ChildItem -Path (Join-Path $docsRoot 'adr') -File | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination (Join-Path $releaseRoot ("adr/{0}" -f $_.Name)) -Force
    }
    Set-Content -Path (Join-Path $releaseRoot 'configuration/production-readiness.json') -Value '{"stream":"AFW-DLV-0011.8","version":"v1.1.0-rc1","configuration":"Release"}'
    Set-Content -Path (Join-Path $releaseRoot 'dashboards/risk-platform-overview.md') -Value @'
# Risk Platform Overview

- Fraud, AML, Risk Scoring, Device, Compliance, Regulatory Reporting.
- Health and metrics endpoints are protected in production-ready host.
'@
}

$decision = if ($failed -eq 0) { 'READY FOR RISK RC' } else { 'NOT READY' }

$summary = [ordered]@{
    stream = 'AFW-DLV-0011.8'
    version = 'v1.1.0-rc1'
    configuration = $Configuration
    generatedAt = (Get-Date).ToString('o')
    checks = $checks.Count
    passed = $passed
    failed = $failed
    skipped = $skipped
    decision = $decision
}
[ordered]@{ summary = $summary; checks = $checks } | ConvertTo-Json -Depth 8 | Set-Content -Path $reportJsonPath

$md = @()
$md += '# Risk & Compliance Release Candidate Validation'
$md += ''
$md += '- Version: v1.1.0-rc1'
$md += '- Stream: AFW-DLV-0011.8'
$md += "- Decision: $decision"
$md += "- Checks: $($checks.Count)"
$md += "- Passed: $passed"
$md += "- Failed: $failed"
$md += "- Skipped: $skipped"
$md += ''
$md += '## Checks'
foreach ($check in $checks) {
    $status = if ($check.Passed) { 'PASS' } else { 'FAIL' }
    $md += "- $($check.Name): $status"
}
$md | Set-Content -Path $reportMdPath

$manifest = [ordered]@{
    stream = 'AFW-DLV-0011.8'
    version = 'v1.1.0-rc1'
    decision = $decision
    generatedAt = (Get-Date).ToString('o')
    files = (Get-ChildItem -Path $releaseRoot -Recurse -File | ForEach-Object { $_.FullName.Substring($repoRoot.Length + 1).Replace('\', '/') } | Sort-Object)
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -Path $manifestPath

$filesForHash = Get-ChildItem -Path $releaseRoot -Recurse -File | Where-Object { $_.FullName -ne $checksumsPath }
$hashes = foreach ($file in $filesForHash) {
    $hash = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $relative = $file.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
    "$hash  $relative"
}
$hashes | Sort-Object | Set-Content -Path $checksumsPath

foreach ($check in $checks) {
    $status = if ($check.Passed) { 'PASS' } else { 'FAIL' }
    $label = ($check.Name + ' ').PadRight(36, '.')
    Write-Host "$label $status"
}
Write-Host ''
Write-Host "Checks: $($checks.Count)"
Write-Host "Passed: $passed"
Write-Host "Failed: $failed"
Write-Host "Skipped: $skipped"
Write-Host ''
Write-Host "Decision: $decision"

if ($failed -gt 0) { exit 1 }
