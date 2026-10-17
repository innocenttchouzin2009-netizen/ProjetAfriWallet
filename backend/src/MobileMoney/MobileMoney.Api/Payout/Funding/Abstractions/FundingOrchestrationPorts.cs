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

public interface IMobileMoneyPayoutAuthoritativeWalletSourceReader
{
    Task<FundingSourceSnapshot?> GetAsync(
        Guid ownerUserId,
        string sourceId,
        string currencyCode,
        CancellationToken cancellationToken = default);
}

public interface IMobileMoneyPayoutFundingAttemptStore
{
    Task SaveAsync(
        FundingAttempt attempt,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<FundingAttempt>> SavePlanAsync(
        IReadOnlyList<FundingAttempt> attempts,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempts);

        if (attempts.Count == 0)
        {
            throw new ArgumentException(
                "At least one funding attempt is required.",
                nameof(attempts));
        }

        foreach (var attempt in attempts)
        {
            await SaveAsync(attempt, cancellationToken);
        }

        return attempts;
    }

    Task<IReadOnlyList<FundingAttempt>> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<FundingAttempt>> SavePlanForOwnerAsync(
        Guid ownerUserId,
        IReadOnlyList<FundingAttempt> attempts,
        CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("Owner user id cannot be empty.", nameof(ownerUserId));

        return await SavePlanAsync(attempts, cancellationToken);
    }

    Task<Guid?> FindOwnerUserIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(null);
}
