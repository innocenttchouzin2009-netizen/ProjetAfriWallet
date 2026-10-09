using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Abstractions;

public interface IMobileMoneyPayoutFundingSourceReader
{
    Task<FundingSourceSnapshot?> GetAsync(
        string sourceId,
        FundingSourceType sourceType,
        CancellationToken cancellationToken = default);
}

public interface IMobileMoneyPayoutFundingAttemptStore
{
    Task SaveAsync(
        FundingAttempt attempt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FundingAttempt>> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
