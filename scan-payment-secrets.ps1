[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repositoryRoot = $PSScriptRoot

$patterns = @(
    '(?i)\b(API[_-]?KEY|CLIENT[_-]?SECRET|ACCESS[_-]?TOKEN|JWT[_-]?SECRET|PASSWORD|PASSWD|PRIVATE[_-]?KEY)\b\s*[:=]\s*["''][^"''\r\n]{8,}["'']',
    'AKIA[0-9A-Z]{16}',
    '-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----',
    '(?i)Bearer\s+[A-Za-z0-9\-\._~\+\/]{20,}=*',
    '(?i)https?://[^/\s:@]+:[^/\s@]+@'
)

$roots = @(
    "backend/src/PaymentPlatform",
    "docs/specs/payment-production-readiness",
    "release/payment-platform/v1.4.0"
)

$extensions = @(
    ".cs",
    ".csproj",
    ".json",
    ".md",
    ".ps1",
    ".xml",
    ".yaml",
    ".yml",
    ".config"
)

$violations = @()

foreach ($relativeRoot in $roots) {
    $root = Join-Path $repositoryRoot $relativeRoot

    if (-not (Test-Path -LiteralPath $root)) {
        continue
    }

    $files = Get-ChildItem -LiteralPath $root -Recurse -File |
        Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and
            $extensions -contains $_.Extension.ToLowerInvariant()
        }

    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction Stop

        foreach ($pattern in $patterns) {
            if ($content -match $pattern) {
                $relativePath = $file.FullName.Substring($repositoryRoot.Length + 1)
                $violations += "${relativePath}: $pattern"
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Output "Secret Scan FAIL"
    $violations | ForEach-Object { Write-Output $_ }
    exit 1
}

Write-Output "Secret Scan PASS"