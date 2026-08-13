$ErrorActionPreference = "Stop"

$projects = @(
    "backend/src/BankingPlatform/BeneficiaryRegistry/BeneficiaryRegistry.Api/BeneficiaryRegistry.Api.csproj",
    "backend/src/BankingPlatform/BankTransferIntent/BankTransferIntent.Api/BankTransferIntent.Api.csproj",
    "backend/src/BankingPlatform/BankRouting/BankRouting.Api/BankRouting.Api.csproj",
    "backend/src/BankingPlatform/BankTransferExecution/BankTransferExecution.Api/BankTransferExecution.Api.csproj",
    "backend/src/BankingPlatform/BankSettlement/BankSettlement.Api/BankSettlement.Api.csproj",
    "backend/src/BankingPlatform/BankProviderIntegration/BankProviderIntegration.Api/BankProviderIntegration.Api.csproj"
)

$failed = $false

foreach ($project in $projects) {
    if (-not (Test-Path $project)) {
        throw "Missing project: $project"
    }

    Write-Host ""
    Write-Host "Scanning $project"

    $output = dotnet list `
      $project `
      package `
      --vulnerable `
      --include-transitive 2>&1

    $output | ForEach-Object {
        Write-Host $_
    }

    if ($LASTEXITCODE -ne 0) {
        $failed = $true
    }

    if ($output -match "has the following vulnerable packages") {
        $failed = $true
    }
}

if ($failed) {
    throw "Banking dependency scan failed."
}

Write-Host ""
Write-Host "Banking dependency scan PASS"
