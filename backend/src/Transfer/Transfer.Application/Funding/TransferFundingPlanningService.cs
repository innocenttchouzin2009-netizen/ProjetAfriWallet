using AfriWallet.Transfer.Domain.Funding;

namespace AfriWallet.Transfer.Application.Funding;

public sealed class TransferFundingPlanningService
{
    private readonly IFundingSourceReader sourceReader;
    private readonly IFundingAttemptStore attemptStore;
    private readonly SplitFundingValidator validator;

    public TransferFundingPlanningService(
        IFundingSourceReader sourceReader,
        IFundingAttemptStore attemptStore,
        SplitFundingValidator validator)
    {
        this.sourceReader = sourceReader ?? throw new ArgumentNullException(nameof(sourceReader));
        this.attemptStore = attemptStore ?? throw new ArgumentNullException(nameof(attemptStore));
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<TransferFundingPlan> PlanAsync(
        PlanTransferFundingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Allocations is null)
        {
            throw new ArgumentException(
                "Funding allocations are required.",
                nameof(command));
        }

        var sources = new List<FundingSourceSnapshot>(command.Allocations.Count);

        foreach (var allocation in command.Allocations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(allocation);

            var source = await sourceReader.GetAsync(
                allocation.SourceId,
                allocation.SourceType,
                cancellationToken);

            if (source is not null)
            {
                sources.Add(source);
            }
        }

        var plan = validator.ValidateAndPlan(command, sources);

        foreach (var allocation in plan.Allocations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attempt = FundingAttempt.Create(
                Guid.NewGuid(),
                plan.CorrelationId,
                allocation,
                plan.PlannedAtUtc);

            await attemptStore.SaveAsync(attempt, cancellationToken);
        }

        return plan;
    }
}
