using MobileMoney.Production.Payout.Quote.Domain;

namespace MobileMoney.Production.Payout.Quote.Abstractions;

public interface IMobileMoneyPayoutQuoteStore
{
    Task SaveAsync(
        MobileMoneyPayoutQuote quote,
        CancellationToken cancellationToken = default);

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
