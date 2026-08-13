$patterns = @(
    'API_KEY\s*=',
    'ACCESS_TOKEN\s*=',
    'CLIENT_SECRET\s*=',
    'JWT_SECRET\s*=',
    'PASSWORD\s*=',
    'Bearer\s+[A-Za-z0-9\-\._~\+\/]+=*'
)

$roots = @(
    "backend/src/OperationsPlatform",
    "docs/specs",
    "release/operations-platform"
)

$excludePathPatterns = @(
    '\\bin\\',
    '\\obj\\'
)

$violations = @()

foreach ($root in $roots) {
    if (-not (Test-Path $root)) {
        continue
    }

    Get-ChildItem `
        -Path $root `
        -Recurse `
        -File |
        Where-Object {
            $path = $_.FullName
            -not ($excludePathPatterns | Where-Object { $path -match $_ })
        } |
        ForEach-Object {
            $content = Get-Content `
                $_.FullName `
                -Raw `
                -ErrorAction SilentlyContinue

            if ([string]::IsNullOrWhiteSpace($content)) {
                return
            }

            foreach ($pattern in $patterns) {
                if ($content -match $pattern) {
                    $violations += "$($_.FullName): $pattern"
                }
            }
        }
}

if ($violations.Count -gt 0) {
    Write-Host "Secret Scan FAIL"

    $violations |
        ForEach-Object {
            Write-Host $_
        }

    exit 1
}

Write-Host "Secret Scan PASS"
