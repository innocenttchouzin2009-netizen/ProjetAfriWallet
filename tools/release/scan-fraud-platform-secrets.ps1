$ErrorActionPreference = 'Stop'
$roots = @('backend/src/Fraud', 'backend/tests/FraudReadiness.Scenarios')
$patterns = @('BEGIN PRIVATE KEY', 'BEGIN RSA PRIVATE KEY', 'BEGIN OPENSSH PRIVATE KEY', 'github_pat_', 'ghp_', 'FRAUD_API_KEY=', 'FRAUD_PROVIDER_SECRET=', 'ENFORCEMENT_SECRET=')
$findings = @()
foreach ($root in $roots) {
    if (-not (Test-Path $root)) { throw "Required path missing: $root" }
    Get-ChildItem $root -Recurse -File | Where-Object { $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' } | ForEach-Object {
        foreach ($pattern in $patterns) { if (Select-String -Path $_.FullName -Pattern $pattern -SimpleMatch -ErrorAction SilentlyContinue) { $findings += "$($_.FullName) -> $pattern" } }
    }
}
if ($findings.Count -gt 0) { Write-Host 'Fraud platform secret scan FAIL'; $findings | ForEach-Object { Write-Host $_ }; exit 1 }
Write-Host 'Fraud platform secret scan PASS'