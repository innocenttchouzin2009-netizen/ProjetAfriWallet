param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$project = Join-Path $root "backend/tests/MerchantReadiness.Scenarios/MerchantReadiness.Scenarios.csproj"
Write-Host "AFW-DLV-0019.7 Merchant Platform Production Readiness"
dotnet build $project -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Merchant readiness build failed." }
dotnet run --project $project -c $Configuration --no-build -- $root
if ($LASTEXITCODE -ne 0) { throw "Merchant readiness scenarios failed." }
& (Join-Path $PSScriptRoot "scan-merchant-readiness-secrets.ps1")
if ($LASTEXITCODE -ne 0) { throw "Merchant readiness secret scan failed." }
Write-Host "Merchant Platform Readiness PASS"