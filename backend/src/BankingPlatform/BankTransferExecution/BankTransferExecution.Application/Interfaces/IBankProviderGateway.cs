using AfriWallet.BankingPlatform.BankTransferExecution.Domain.Providers;

namespace AfriWallet.BankingPlatform.BankTransferExecution.Application.Interfaces;

public interface IBankProviderGateway
{
    Task<BankProviderExecutionResult> ExecuteAsync(
        Guid executionId,
        string providerCode,
        string railCode,
        long amountMinor,
        string currencyCode,
        CancellationToken cancellationToken);
}
