using AfriWallet.BankingPlatform.BankTransferExecution.Application.Interfaces;

namespace AfriWallet.BankingPlatform.BankTransferExecution.Infrastructure.Gateways;

public sealed class SandboxTransferIntentGateway
    : ITransferIntentGateway
{
    public Task<TransferIntentExecutionEligibility>
        GetEligibilityAsync(
            Guid transferIntentId,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            new TransferIntentExecutionEligibility(
                Exists: transferIntentId != Guid.Empty,
                ReadyForRouting: true,
                AmountMinor: 50_000,
                CurrencyCode: "EUR"));
    }
}
