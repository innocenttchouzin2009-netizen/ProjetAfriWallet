using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Domain;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Execution.Infrastructure;

public sealed class StoreBackedMobileMoneyPayoutExecutionFundingBindingReader
    : IMobileMoneyPayoutExecutionFundingBindingReader
{
    private readonly IMobileMoneyPayoutFundingAttemptStore _attemptStore;

    public StoreBackedMobileMoneyPayoutExecutionFundingBindingReader(
        IMobileMoneyPayoutFundingAttemptStore attemptStore)
    {
        _attemptStore = attemptStore ??
            throw new ArgumentNullException(nameof(attemptStore));
    }

    public async Task<MobileMoneyPayoutExecutionFundingBinding?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Funding correlation id cannot be empty.",
                nameof(correlationId));
        }

        var attempts = await _attemptStore.FindByCorrelationIdAsync(
            correlationId,
            cancellationToken);

        if (attempts.Count == 0)
            return null;

        if (attempts.Any(attempt => attempt.Status != FundingAttemptStatus.Planned))
        {
            throw new InvalidOperationException(
                "Only planned funding attempts can be bound for payout execution.");
        }

        var orderedAttempts = attempts
            .OrderBy(attempt => attempt.CreatedAtUtc)
            .ThenBy(attempt => attempt.Id)
            .ToArray();

        var allocations = orderedAttempts
            .Select(attempt =>
                MobileMoneyPayoutExecutionFundingAllocation.Create(
                    attempt.Allocation.SourceId,
                    attempt.Allocation.SourceType,
                    attempt.Allocation.AmountMinor,
                    attempt.Allocation.CurrencyCode))
            .ToArray();

        var requiredAmountMinor = allocations.Aggregate(
            0L,
            (total, allocation) => checked(total + allocation.AmountMinor));

        return MobileMoneyPayoutExecutionFundingBinding.Create(
            correlationId,
            requiredAmountMinor,
            allocations[0].CurrencyCode,
            allocations);
    }
}
