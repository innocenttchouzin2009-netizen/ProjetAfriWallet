using AfriWallet.Transfer.Domain.Funding;

namespace AfriWallet.Transfer.Application.Funding;

public interface IFundingSourceReader
{
    Task<FundingSourceSnapshot?> GetAsync(
        string sourceId,
        FundingSourceType sourceType,
        CancellationToken cancellationToken = default);
}

public interface IFundingAttemptStore
{
    Task SaveAsync(
        FundingAttempt attempt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FundingAttempt>> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
