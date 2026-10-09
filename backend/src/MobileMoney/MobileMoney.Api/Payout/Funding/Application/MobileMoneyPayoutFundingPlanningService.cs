using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Application;

public sealed class MobileMoneyPayoutFundingPlanningService
{
    private readonly IMobileMoneyPayoutFundingSourceReader _sourceReader;
    private readonly IMobileMoneyPayoutFundingAttemptStore _attemptStore;
    private readonly MobileMoneyPayoutSplitFundingValidator _validator;

    public MobileMoneyPayoutFundingPlanningService(
        IMobileMoneyPayoutFundingSourceReader sourceReader,
        IMobileMoneyPayoutFundingAttemptStore attemptStore,
        MobileMoneyPayoutSplitFundingValidator validator)
    {
        _sourceReader = sourceReader ??
            throw new ArgumentNullException(nameof(sourceReader));
        _attemptStore = attemptStore ??
            throw new ArgumentNullException(nameof(attemptStore));
        _validator = validator ??
            throw new ArgumentNullException(nameof(validator));
    }

    public async Task<MobileMoneyPayoutFundingPlan> PlanAsync(
        PlanMobileMoneyPayoutFundingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Allocations is null)
        {
            throw new ArgumentException(
                "Funding allocations are required.",
                nameof(command));
        }

        var sources =
            new List<FundingSourceSnapshot>(command.Allocations.Count);

        foreach (var allocation in command.Allocations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(allocation);

            var source = await _sourceReader.GetAsync(
                allocation.SourceId,
                allocation.SourceType,
                cancellationToken);

            if (source is not null)
            {
                sources.Add(source);
            }
        }

        var plan = _validator.ValidateAndPlan(command, sources);

        foreach (var allocation in plan.Allocations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attempt = FundingAttempt.Create(
                Guid.NewGuid(),
                plan.CorrelationId,
                allocation,
                plan.PlannedAtUtc);

            await _attemptStore.SaveAsync(attempt, cancellationToken);
        }

        return plan;
    }
}
