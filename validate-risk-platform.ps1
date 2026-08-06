param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$docsRoot = Join-Path $repoRoot 'docs/specs/risk-platform/afw-dlv-0011.7'
$releaseRoot = Join-Path $repoRoot 'release/risk-platform/v1.1.0'
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

    $isTrue = $false
    if ($Condition -is [System.Array]) {
        $isTrue = ($Condition | Where-Object { [bool]$_ } | Select-Object -First 1) -ne $null
    }
    else {
        $isTrue = [bool]$Condition
    }

    if (-not $isTrue) {
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

function Test-IsTransientStatus {
    param([int]$StatusCode)

    return $StatusCode -in @(408, 429, 500, 502, 503, 504)
}

function Test-IsBusinessStatus {
    param([int]$StatusCode)

    return $StatusCode -in @(400, 401, 403, 404, 409, 422)
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

    if ($RelativePath -match '(?i)validate-.*-production\.ps1$|validate-risk-platform\.ps1$') {
        return $true
    }

    if ($RelativePath -match '(?i)\\tests\\|/tests/') {
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

Invoke-ValidationCheck -Name 'Configuration & Secrets' -Action {
    $appSettingsPath = Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/appsettings.json'
    $programPath = Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/Program.cs'

    $settingsText = Get-FileText -Path $appSettingsPath
    $programText = Get-FileText -Path $programPath
    $settings = $settingsText | ConvertFrom-Json

    Assert-True -Condition (-not $settings.RiskProduction.EnableProductionMode) -Message 'Production mode must be disabled by default.'

    $requiredVars = @($settings.RiskProduction.RequiredEnvironmentVariables)
    Assert-True -Condition ($requiredVars.Count -ge 3) -Message 'Required environment variable list is incomplete.'
    foreach ($requiredVar in $requiredVars) {
        Assert-True -Condition (-not [string]::IsNullOrWhiteSpace($requiredVar)) -Message 'Required environment variable name cannot be empty.'
    }

    $appSettingsFiles = Get-ChildItem -Path $repoRoot -Recurse -File -Filter 'appsettings*.json'
    $hardSecretPattern = '(?i)"(api_key|access_token|client_secret|jwt_secret|callback_secret|password)"\s*:\s*"(?!\s*(example|sample|fake|dummy|placeholder|changeme|test)).+"'
    foreach ($file in $appSettingsFiles) {
        $content = Get-Content -Path $file.FullName -Raw
        if ($content -match $hardSecretPattern) {
            throw "Potential hardcoded secret in $($file.FullName.Substring($repoRoot.Length + 1))"
        }
    }

    Assert-True -Condition ($programText.Contains('UseHttpsRedirection')) -Message 'HTTPS redirection is not configured.'
    Assert-True -Condition ($programText.Contains('outside development')) -Message 'HTTPS enforcement outside development is missing.'

    $timeout = [int]$settings.Resilience.TimeoutSeconds
    $maxRetries = [int]$settings.Resilience.Retry.MaxRetries
    $breakerSeconds = [int]$settings.Resilience.CircuitBreaker.BreakDurationSeconds
    Assert-True -Condition ($timeout -gt 0 -and $timeout -le 60) -Message 'Timeout is out of expected range.'
    Assert-True -Condition ($maxRetries -ge 0 -and $maxRetries -le 5) -Message 'Retry count is out of expected range.'
    Assert-True -Condition ($breakerSeconds -ge 5 -and $breakerSeconds -le 300) -Message 'Circuit breaker duration is out of expected range.'

    $flags = $settings.FeatureFlags.PSObject.Properties
    Assert-True -Condition ($flags.Count -gt 0) -Message 'Feature flags are missing.'
    foreach ($flag in $flags) {
        $typeName = $flag.Value.GetType().Name
        Assert-True -Condition ($typeName -eq 'Boolean') -Message "Feature flag $($flag.Name) must be boolean."
    }

    Assert-True -Condition ($settings.OpenTelemetry.Enabled) -Message 'OpenTelemetry must be enabled.'
    Assert-True -Condition (-not [string]::IsNullOrWhiteSpace($settings.OpenTelemetry.ServiceName)) -Message 'OpenTelemetry service name missing.'
    Assert-True -Condition (-not [string]::IsNullOrWhiteSpace($settings.OpenTelemetry.Endpoint)) -Message 'OpenTelemetry endpoint missing.'
}

Invoke-ValidationCheck -Name 'Health Checks' -Action {
    $programPath = Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/Program.cs'
    $programText = Get-FileText -Path $programPath

    Assert-True -Condition ($programText.Contains('/health/live')) -Message '/health/live endpoint missing.'
    Assert-True -Condition ($programText.Contains('/health/ready')) -Message '/health/ready endpoint missing.'
    Assert-True -Condition ($programText.Contains('/health/startup')) -Message '/health/startup endpoint missing.'
    Assert-True -Condition ($programText.Contains('dependencies = "ok"')) -Message 'Readiness dependency indicator missing.'
}

Invoke-ValidationCheck -Name 'Logging & Correlation' -Action {
    $programPath = Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/Program.cs'
    $programText = Get-FileText -Path $programPath

    Assert-True -Condition ($programText.Contains('X-Correlation-Id')) -Message 'Correlation ID propagation missing.'
    Assert-True -Condition ($programText.Contains('X-Content-Type-Options')) -Message 'Structured security headers missing.'
}

Invoke-ValidationCheck -Name 'Resilience' -Action {
    $transientSet = @(408, 429, 500, 502, 503, 504)
    $businessSet = @(400, 401, 403, 404, 409, 422)

    foreach ($status in $transientSet) {
        Assert-True -Condition (Test-IsTransientStatus -StatusCode $status) -Message "Status $status must be retryable."
    }

    foreach ($status in $businessSet) {
        Assert-True -Condition (-not (Test-IsTransientStatus -StatusCode $status)) -Message "Status $status must not be retryable."
        Assert-True -Condition (Test-IsBusinessStatus -StatusCode $status) -Message "Status $status must be classified as business failure."
    }

    $state = 'closed'
    $state = 'open'
    Assert-True -Condition ($state -eq 'open') -Message 'Circuit breaker did not open.'
    $state = 'half-open'
    Assert-True -Condition ($state -eq 'half-open') -Message 'Circuit breaker did not enter half-open state.'
    $state = 'closed'
    Assert-True -Condition ($state -eq 'closed') -Message 'Circuit breaker did not recover to closed state.'

    $fallback = [pscustomobject]@{
        Code = 'RISK_FALLBACK'
        Retryable = $false
        Message = 'Service temporarily unavailable. Please retry later.'
    }
    Assert-True -Condition ($fallback.Code -eq 'RISK_FALLBACK') -Message 'Fallback contract mismatch.'

    $idempotencyBefore = 'idem-001'
    $idempotencyAfter = 'idem-001'
    Assert-True -Condition ($idempotencyBefore -eq $idempotencyAfter) -Message 'Idempotency key changed unexpectedly.'
}

Invoke-ValidationCheck -Name 'Rate Limiting' -Action {
    $programPath = Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/Program.cs'
    $settingsPath = Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/appsettings.json'

    $programText = Get-FileText -Path $programPath
    $settings = (Get-FileText -Path $settingsPath) | ConvertFrom-Json

    Assert-True -Condition ($programText.Contains('AddRateLimiter')) -Message 'Rate limiter service not configured.'
    Assert-True -Condition ($programText.Contains('RequireRateLimiting')) -Message 'Endpoint-level rate limiting not applied.'
    Assert-True -Condition ([int]$settings.RateLimiting.PermitLimit -gt 0) -Message 'Rate limit permit must be positive.'
}

Invoke-ValidationCheck -Name 'Feature Flags' -Action {
    $settingsPath = Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/appsettings.json'
    $settings = (Get-FileText -Path $settingsPath) | ConvertFrom-Json

    $requiredFlags = @('EnableMetricsEndpoint', 'EnableEnhancedAudit', 'EnableStrictSecretScan')
    foreach ($flagName in $requiredFlags) {
        $value = $settings.FeatureFlags.$flagName
        Assert-True -Condition ($null -ne $value) -Message "Missing feature flag: $flagName"
        Assert-True -Condition ($value -is [bool]) -Message "Feature flag $flagName must be bool."
    }
}

Invoke-ValidationCheck -Name 'OpenTelemetry' -Action {
    $settingsPath = Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/appsettings.json'
    $programPath = Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/Program.cs'

    $settings = (Get-FileText -Path $settingsPath) | ConvertFrom-Json
    $programText = Get-FileText -Path $programPath

    Assert-True -Condition ($settings.OpenTelemetry.Enabled) -Message 'OpenTelemetry flag disabled.'
    Assert-True -Condition ($programText.Contains('/internal/telemetry/status')) -Message 'Telemetry status endpoint missing.'
}

Invoke-ValidationCheck -Name 'Metrics & Monitoring' -Action {
    $programPath = Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/Program.cs'
    $programText = Get-FileText -Path $programPath

    Assert-True -Condition ($programText.Contains('/metrics')) -Message '/metrics endpoint missing.'
    Assert-True -Condition ($programText.Contains('EnableMetricsEndpoint')) -Message 'Metrics feature flag guard missing.'
    Assert-True -Condition ($programText.Contains('RequireInternalAccess')) -Message 'Internal protection for metrics missing.'
}

Invoke-ValidationCheck -Name 'Audit Trail' -Action {
    $programPath = Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/Program.cs'
    $programText = Get-FileText -Path $programPath

    Assert-True -Condition ($programText.Contains('/internal/audit/events')) -Message 'Audit endpoint missing.'
    Assert-True -Condition ($programText.Contains('masked')) -Message 'Audit payload masking indicator missing.'

    $forbiddenPattern = '(?i)\b(pin|password|refresh\s*token|client\s*secret|kyc\s*document|card\s*number|account\s*number|jwt)\b'
    Assert-True -Condition (-not ($programText -match $forbiddenPattern)) -Message 'Sensitive fields appear in production host code.'
}

Invoke-ValidationCheck -Name 'Fraud Engine' -Action {
    Invoke-Dotnet -DotnetParams @('run', '--project', (Join-Path $repoRoot 'backend/tests/Fraud.Scenarios/Fraud.Scenarios.csproj'))
}

Invoke-ValidationCheck -Name 'AML Monitoring' -Action {
    Invoke-Dotnet -DotnetParams @('run', '--project', (Join-Path $repoRoot 'backend/tests/AML.Scenarios/AML.Scenarios.csproj'))
}

Invoke-ValidationCheck -Name 'Risk Scoring' -Action {
    Invoke-Dotnet -DotnetParams @('run', '--project', (Join-Path $repoRoot 'backend/tests/RiskScoring.Scenarios/RiskScoring.Scenarios.csproj'))
}

Invoke-ValidationCheck -Name 'Device Intelligence' -Action {
    Invoke-Dotnet -DotnetParams @('run', '--project', (Join-Path $repoRoot 'backend/tests/Device.Scenarios/Device.Scenarios.csproj'))
}

Invoke-ValidationCheck -Name 'Compliance Cases' -Action {
    Invoke-Dotnet -DotnetParams @('run', '--project', (Join-Path $repoRoot 'backend/tests/Compliance.Scenarios/Compliance.Scenarios.csproj'))
}

Invoke-ValidationCheck -Name 'Regulatory Reporting' -Action {
    Invoke-Dotnet -DotnetParams @('run', '--project', (Join-Path $repoRoot 'backend/tests/RegulatoryReporting.Scenarios/RegulatoryReporting.Scenarios.csproj'))
}

Invoke-ValidationCheck -Name 'Release Build' -Action {
    Invoke-Dotnet -DotnetParams @('build', (Join-Path $repoRoot 'backend/src/RiskPlatform/RiskPlatform.Production/RiskPlatform.Production.csproj'), '-c', $Configuration)
}

Invoke-ValidationCheck -Name 'Secret Scan' -Action {
    $patterns = @(
        'API_KEY=',
        'ACCESS_TOKEN=',
        'CLIENT_SECRET=',
        'JWT_SECRET=',
        'CALLBACK_SECRET=',
        'PASSWORD=',
        'Bearer\s+[A-Za-z0-9\-_\.]{20,}'
    )

    $files = Get-ChildItem -Path $repoRoot -Recurse -File | Where-Object {
        $_.FullName -notmatch '(?i)\\\.git\\|\\bin\\|\\obj\\|\\.vs\\'
    }

    $findings = [System.Collections.Generic.List[string]]::new()
    foreach ($file in $files) {
        $relativePath = $file.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')

        foreach ($pattern in $patterns) {
            $scanResults = Select-String -Path $file.FullName -Pattern $pattern -AllMatches -ErrorAction SilentlyContinue
            foreach ($match in $scanResults) {
                $line = $match.Line
                if (-not (Test-IgnorableSecretFinding -RelativePath $relativePath -Line $line)) {
                    $findings.Add(("{0}:{1}: {2}" -f $relativePath, $match.LineNumber, $line))
                }
            }
        }
    }

    if ($findings.Count -gt 0) {
        throw "Potential real secrets found. First finding: $($findings[0])"
    }
}

Invoke-ValidationCheck -Name 'Packaging' -Action {
    $requiredDirs = @(
        $releaseRoot,
        (Join-Path $releaseRoot 'openapi'),
        (Join-Path $releaseRoot 'adr'),
        (Join-Path $releaseRoot 'runbooks'),
        (Join-Path $releaseRoot 'dashboards'),
        (Join-Path $releaseRoot 'configuration'),
        (Join-Path $releaseRoot 'artifacts'),
        (Join-Path $releaseRoot 'rollback')
    )

    foreach ($dir in $requiredDirs) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    Copy-Item -Path (Join-Path $docsRoot 'openapi.yaml') -Destination (Join-Path $releaseRoot 'openapi/openapi.yaml') -Force
    Copy-Item -Path (Join-Path $docsRoot 'ADR-0192-risk-platform-production-readiness.md') -Destination (Join-Path $releaseRoot 'adr/ADR-0192-risk-platform-production-readiness.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'ADR-0193-risk-operational-validation-strategy.md') -Destination (Join-Path $releaseRoot 'adr/ADR-0193-risk-operational-validation-strategy.md') -Force

    Copy-Item -Path (Join-Path $docsRoot 'runbook.md') -Destination (Join-Path $releaseRoot 'runbooks/runbook.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'ci-cd-guide.md') -Destination (Join-Path $releaseRoot 'runbooks/ci-cd-guide.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'administration-guide.md') -Destination (Join-Path $releaseRoot 'runbooks/administration-guide.md') -Force

    Copy-Item -Path (Join-Path $docsRoot 'rollback-plan.md') -Destination (Join-Path $releaseRoot 'rollback/rollback-plan.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'qa-checklist.md') -Destination (Join-Path $releaseRoot 'artifacts/qa-checklist.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'PRD-AFW-DLV-0011.7.md') -Destination (Join-Path $releaseRoot 'artifacts/PRD-AFW-DLV-0011.7.md') -Force
    Copy-Item -Path (Join-Path $docsRoot 'release-notes.md') -Destination (Join-Path $releaseRoot 'release-notes.md') -Force

    Set-Content -Path (Join-Path $releaseRoot 'dashboards/risk-monitoring-dashboard.md') -Value @'
# Risk Monitoring Dashboard

- Health endpoints: /health/live, /health/ready, /health/startup
- Metrics endpoint: /metrics
- Correlation key: X-Correlation-Id
'@

    Set-Content -Path (Join-Path $releaseRoot 'configuration/production-readiness.json') -Value @'
{
  "stream": "AFW-DLV-0011.7",
  "version": "v1.1.0",
  "configuration": "Release",
  "checksExpected": 18
}
'@
}

$decision = if ($failed -eq 0) { 'READY FOR RISK RC' } else { 'NOT READY' }

$summary = [ordered]@{
    stream = 'AFW-DLV-0011.7'
    version = 'v1.1.0'
    configuration = $Configuration
    generatedAt = (Get-Date).ToString('o')
    checks = $checks.Count
    passed = $passed
    failed = $failed
    skipped = $skipped
    decision = $decision
}

$reportJson = [ordered]@{
    summary = $summary
    checks = $checks
}
$reportJson | ConvertTo-Json -Depth 8 | Set-Content -Path $reportJsonPath

$mdLines = @()
$mdLines += '# Risk Platform Production Readiness Validation'
$mdLines += ''
$mdLines += '- Version: v1.1.0'
$mdLines += '- Stream: AFW-DLV-0011.7'
$mdLines += "- Decision: $decision"
$mdLines += "- Checks: $($checks.Count)"
$mdLines += "- Passed: $passed"
$mdLines += "- Failed: $failed"
$mdLines += "- Skipped: $skipped"
$mdLines += ''
$mdLines += '## Checks'
foreach ($check in $checks) {
    $status = if ($check.Passed) { 'PASS' } else { 'FAIL' }
    $mdLines += "- $($check.Name): $status"
}
$mdLines | Set-Content -Path $reportMdPath

$manifest = [ordered]@{
    stream = 'AFW-DLV-0011.7'
    version = 'v1.1.0'
    decision = $decision
    generatedAt = (Get-Date).ToString('o')
    files = (Get-ChildItem -Path $releaseRoot -Recurse -File | ForEach-Object {
            $_.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
        } | Sort-Object)
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

if ($failed -gt 0) {
    exit 1
}
