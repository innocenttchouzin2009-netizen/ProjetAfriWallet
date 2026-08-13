namespace AfriWallet.BankingPlatform.BankTransferExecution.Application.Interfaces;

public interface ITransferIntentGateway
{
    Task<TransferIntentExecutionEligibility>
        GetEligibilityAsync(
            Guid transferIntentId,
            CancellationToken cancellationToken);
}

public sealed record TransferIntentExecutionEligibility(
    bool Exists,
    bool ReadyForRouting,
    long AmountMinor,
    string CurrencyCode);
