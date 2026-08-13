param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

Write-Host ''
Write-Host 'AFW-DLV-0015.7 - Banking Platform Production Readiness'
Write-Host ''

function Invoke-Step {
    param(
        [string]$Name,
        [string]$Executable,
        [string[]]$Arguments
    )

    Write-Host ''
    Write-Host ("==> $Name")

    & $Executable @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }

    Write-Host ($Name + ' PASS')
}

$steps = @(
    @{ Name = '0015.1 Beneficiary Registry'; Executable = 'dotnet'; Arguments = @('run', '--project', 'backend/tests/BeneficiaryRegistry.Scenarios/BeneficiaryRegistry.Scenarios.csproj', '-c', $Configuration) },
    @{ Name = '0015.2 Transfer Intent'; Executable = 'dotnet'; Arguments = @('run', '--project', 'backend/tests/BankTransferIntent.Scenarios/BankTransferIntent.Scenarios.csproj', '-c', $Configuration) },
    @{ Name = '0015.3 Routing & Rail Selection'; Executable = 'dotnet'; Arguments = @('run', '--project', 'backend/tests/BankRouting.Scenarios/BankRouting.Scenarios.csproj', '-c', $Configuration) },
    @{ Name = '0015.4 Transfer Execution'; Executable = 'dotnet'; Arguments = @('run', '--project', 'backend/tests/BankTransferExecution.Scenarios/BankTransferExecution.Scenarios.csproj', '-c', $Configuration) },
    @{ Name = '0015.5 Settlement & Reconciliation'; Executable = 'dotnet'; Arguments = @('run', '--project', 'backend/tests/BankSettlement.Scenarios/BankSettlement.Scenarios.csproj', '-c', $Configuration) },
    @{ Name = '0015.6 Provider Integration'; Executable = 'dotnet'; Arguments = @('run', '--project', 'backend/tests/BankProviderIntegration.Scenarios/BankProviderIntegration.Scenarios.csproj', '-c', $Configuration) },
    @{ Name = 'Banking Readiness Build'; Executable = 'dotnet'; Arguments = @('build', 'backend/src/BankingPlatform/BankingReadiness/BankingReadiness.csproj', '-c', $Configuration, '-nologo') },
    @{ Name = 'Banking Readiness Runner'; Executable = 'dotnet'; Arguments = @('run', '--project', 'backend/src/BankingPlatform/BankingReadiness/BankingReadiness.csproj', '-c', $Configuration) },
    @{ Name = 'Banking Readiness Scenarios'; Executable = 'dotnet'; Arguments = @('run', '--project', 'backend/tests/BankingReadiness.Scenarios/BankingReadiness.Scenarios.csproj', '-c', $Configuration) },
    @{ Name = 'Banking Secret Scan'; Executable = 'powershell'; Arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', 'tools/release/scan-banking-secrets.ps1') },
    @{ Name = 'Banking Dependency Scan'; Executable = 'powershell'; Arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', 'tools/release/scan-banking-dependencies.ps1') }
)

foreach ($step in $steps) {
    Invoke-Step -Name $step.Name -Executable $step.Executable -Arguments $step.Arguments
}

Write-Host ''
Write-Host 'All AFW-DLV-0015.7 banking readiness validations passed.'
