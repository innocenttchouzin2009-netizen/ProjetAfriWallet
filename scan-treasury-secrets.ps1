$patterns = @(
    'API_KEY\s*=',
    'ACCESS_TOKEN\s*=',
    'CLIENT_SECRET\s*=',
    'JWT_SECRET\s*=',
    'PASSWORD\s*=',
    'Bearer\s+[A-Za-z0-9\-\._~\+\/]+=*'
)

$roots = @(
    'backend/src/FinancialPlatform',
    'docs/specs/treasury-production-readiness',
    'docs/specs/treasury-release-candidate',
    'release/financial-platform/v1.3.0-rc1'
)

$violations = @()

foreach ($root in $roots) {
    if (-not (Test-Path $root)) {
        continue
    }

    Get-ChildItem -Path $root -Recurse -File |
        Where-Object {
            $_.FullName -notmatch '\\bin\\' -and
            $_.FullName -notmatch '\\obj\\'
        } |
        ForEach-Object {
            $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue

            foreach ($pattern in $patterns) {
                if ($content -match $pattern) {
                    $violations += "$($_.FullName): $pattern"
                }
            }
        }
}

if ($violations.Count -gt 0) {
    Write-Host 'Secret Scan FAIL'
    $violations | ForEach-Object { Write-Host $_ }
    exit 1
}

Write-Host 'Secret Scan PASS'