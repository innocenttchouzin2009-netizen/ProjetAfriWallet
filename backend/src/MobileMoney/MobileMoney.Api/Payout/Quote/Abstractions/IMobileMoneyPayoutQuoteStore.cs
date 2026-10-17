using MobileMoney.Production.Payout.Quote.Domain;

namespace MobileMoney.Production.Payout.Quote.Abstractions;

public interface IMobileMoneyPayoutQuoteStore
{
    Task SaveAsync(
        MobileMoneyPayoutQuote quote,
        CancellationToken cancellationToken = default);

    async Task SaveOwnedAsync(
        Guid ownerUserId,
        MobileMoneyPayoutQuote quote,
        CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("Owner user id cannot be empty.", nameof(ownerUserId));

        await SaveAsync(quote, cancellationToken);
    }

    Task<Guid?> FindOwnerUserIdAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(null);

    Task<MobileMoneyPayoutQuote?> FindByIdAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default);

    Task LinkFundingCorrelationAsync(
        Guid quoteId,
        Guid fundingCorrelationId,
        CancellationToken cancellationToken = default);

    Task<Guid?> FindFundingCorrelationIdAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default);

    Task<Guid?> FindQuoteIdByFundingCorrelationIdAsync(
        Guid fundingCorrelationId,
        CancellationToken cancellationToken = default);
}
