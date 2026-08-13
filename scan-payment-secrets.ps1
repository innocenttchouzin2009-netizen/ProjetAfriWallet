$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$patterns = @(
    '(?i)(?:api[_-]?key|access[_-]?token|client[_-]?secret|jwt[_-]?secret|callback[_-]?secret|password)\s*[:=]\s*["''](?!(?:example|sample|fake|dummy|placeholder|changeme|test)).+',
    '(?i)(?:AKIA|ASIA)[A-Z0-9]{16}',
    '(?i)AIza[0-9A-Za-z\-_]{35}',
    '(?i)sk_(?:live|test)_[A-Za-z0-9]{16,}',
    '(?i)Bearer\s+[A-Za-z0-9._\-]{20,}'
)

$files = Get-ChildItem -Path $repoRoot -Recurse -File |
    Where-Object {
        $_.FullName -notmatch '(?i)\\(\\.git|bin|obj|\.vs)\\' -and
        $_.Extension -in '.cs', '.json', '.ps1', '.yaml', '.yml', '.csproj'
    }

$findings = [System.Collections.Generic.List[string]]::new()

foreach ($file in $files) {
    $relative = $file.FullName.Substring($repoRoot.Length + 1).Replace('\\', '/')
    if ($relative -match '(?i)(?:build-risk-release\.ps1|validate-risk-platform\.ps1|README|CHANGELOG|SECURITY|\.md)$') {
        continue
    }

    $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) { continue }

    $lines = $content -split "`r?`n"
    foreach ($line in $lines) {
        foreach ($pattern in $patterns) {
            if ($line -match $pattern) {
                if ($line -match '(?i)(?:api[_-]?key|access[_-]?token|client[_-]?secret|jwt[_-]?secret|callback[_-]?secret|password)\s*[:=]\s*["'']\s*["'']') {
                    continue
                }

                if ($line -match '(?i)(?:api[_-]?key|access[_-]?token|client[_-]?secret|jwt[_-]?secret|callback[_-]?secret|password)\s*[:=]\s*\$\{?\w+\}?') {
                    continue
                }

                if ($line -match '(?i)[A-Z0-9_]{4,}_(?:API_KEY|ACCESS_TOKEN|CLIENT_SECRET|JWT_SECRET|CALLBACK_SECRET|PASSWORD)') {
                    continue
                }

                if ($line -match '(?i)public\s+const\s+string\s+[A-Za-z0-9_]+\s*=\s*"[A-Z0-9_]+";') {
                    continue
                }

                $findings.Add("$relative : $line")
            }
        }
    }
}

if ($findings.Count -gt 0) {
    foreach ($finding in $findings | Select-Object -First 10) {
        Write-Host "SECRET_FINDING: $finding"
    }
    throw "Secret scan found potential secrets in the repository."
}

Write-Host 'Payment Secret Scan PASS'
