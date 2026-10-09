using System.Collections.Concurrent;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Infrastructure;

public sealed class InMemoryMobileMoneyPayoutFundingAttemptStore
    : IMobileMoneyPayoutFundingAttemptStore
{
    private readonly ConcurrentDictionary<Guid, FundingAttempt> _attempts =
        new();

    public Task SaveAsync(
        FundingAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        cancellationToken.ThrowIfCancellationRequested();

        _attempts[attempt.Id] = attempt;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FundingAttempt>> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<FundingAttempt> result = _attempts.Values
            .Where(attempt => attempt.CorrelationId == correlationId)
            .OrderBy(attempt => attempt.CreatedAtUtc)
            .ToArray();

        return Task.FromResult(result);
    }
}
