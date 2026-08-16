$ErrorActionPreference = 'Stop'

$roots = @(
    '.\backend',
    '.\tools',
    '.\docs',
    '.\.github'
)

$patterns = @(
    'ghp_',
    'github_pat_',
    'sk_live_',
    'KYC_API_KEY=',
    'KYC_CLIENT_SECRET=',
    'IDENTITY_PROVIDER_SECRET='
)

$violations = @()

foreach ($root in $roots)
{
    if (-not (Test-Path $root))
    {
        continue
    }

    Get-ChildItem -Path $root -Recurse -File |
        Where-Object {
            $_.FullName -notmatch '\\bin\\' -and
            $_.FullName -notmatch '\\obj\\' -and
            $_.Name -notmatch '^scan-.*\.ps1$' -and
            $_.Name -notmatch '^scan-banking-secrets\.ps1$'
        } |
        ForEach-Object {
            foreach ($pattern in $patterns)
            {
                $matches = Select-String -Path $_.FullName -Pattern $pattern -SimpleMatch -ErrorAction SilentlyContinue
                if ($matches)
                {
                    $violations += "$($_.FullName) -> $pattern"
                }
            }
        }
}

if ($violations.Count -gt 0)
{
    Write-Host 'Identity verification secret scan FAIL'
    $violations | ForEach-Object { Write-Host $_ }
    exit 1
}

Write-Host 'Identity verification secret scan PASS'
