namespace Liquidity.Application.Interfaces;

public interface ITreasuryReadModel
{
    Task<IReadOnlyCollection<TreasuryLiquidityAccountState>> GetAccountStatesAsync(
        CancellationToken cancellationToken);
}

public sealed record TreasuryLiquidityAccountState(
    Guid AccountId,
    string CurrencyCode,
    long LedgerNetMinor,
    long ReservedMinor,
    long PendingMinor,
    long BlockedMinor);
