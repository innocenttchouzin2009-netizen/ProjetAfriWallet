namespace Settlement.Application.Interfaces;

public sealed record TreasurySettlementPosting(
    Guid InstructionId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    string SourceCurrency,
    string DestinationCurrency,
    long SourceAmountMinor,
    long DestinationAmountMinor,
    decimal AppliedRate);

public interface ITreasurySettlementGateway
{
    Task<bool> HasAvailableFundsAsync(
        Guid accountId,
        string currencyCode,
        long amountMinor,
        CancellationToken cancellationToken);

    Task PostSettlementAsync(TreasurySettlementPosting posting, CancellationToken cancellationToken);
}
