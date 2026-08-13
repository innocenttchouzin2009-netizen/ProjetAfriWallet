using AfriWallet.BankingPlatform.BankTransferExecution.Application.Interfaces;
using AfriWallet.BankingPlatform.BankTransferExecution.Domain.Providers;

namespace AfriWallet.BankingPlatform.BankTransferExecution.Infrastructure.Gateways;

public sealed class SandboxBankProviderGateway
    : IBankProviderGateway
{
    public Task<BankProviderExecutionResult> ExecuteAsync(
        Guid executionId,
        string providerCode,
        string railCode,
        long amountMinor,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (providerCode.Equals(
                "FAIL-SANDBOX",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                new BankProviderExecutionResult(
                    false,
                    null,
                    "sandbox_provider_failure",
                    true));
        }

        return Task.FromResult(
            new BankProviderExecutionResult(
                true,
                $"bank-{providerCode}-{executionId:N}",
                null,
                false));
    }
}
