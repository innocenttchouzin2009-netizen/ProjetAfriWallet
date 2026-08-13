$ErrorActionPreference = "Stop"

$roots = @(
    "backend/src/BankingPlatform",
    "backend/tests",
    "docs/specs/banking-production-readiness",
    "release/banking-platform/v1.5.0"
)

$patterns = @(
    "BEGIN PRIVATE KEY",
    "BEGIN RSA PRIVATE KEY",
    "ghp_",
    "github_pat_",
    "sk_live_",
    "AKIA",
    "BANK_API_KEY=",
    "BANK_CLIENT_SECRET=",
    "SWIFT_PASSWORD="
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
        $_.FullName -notmatch '\\bin\\' -and
        $_.FullName -notmatch '\\obj\\'
    } |
    ForEach-Object {
        $path = $_.FullName

        foreach ($pattern in $patterns) {
            $match = Select-String `
              -Path $path `
              -SimpleMatch `
              -Pattern $pattern `
              -ErrorAction SilentlyContinue

            if ($match) {
                $violations += "$path -> $pattern"
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Banking secret scan FAIL"
    $violations | ForEach-Object {
        Write-Host $_
    }
    exit 1
}

Write-Host "Banking secret scan PASS"
