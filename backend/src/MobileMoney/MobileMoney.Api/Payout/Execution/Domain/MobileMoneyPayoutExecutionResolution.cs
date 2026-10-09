namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionResolution
{
    private MobileMoneyPayoutExecutionResolution(
        bool isReplay,
        MobileMoneyPayoutExecutionBinding? binding,
        MobileMoneyPayoutExecutionIdempotencyEntry idempotencyEntry)
    {
        IsReplay = isReplay;
        Binding = binding;
        IdempotencyEntry = idempotencyEntry;
    }

    public bool IsReplay { get; }
    public bool ShouldExecute => !IsReplay;
    public MobileMoneyPayoutExecutionBinding? Binding { get; }
    public MobileMoneyPayoutExecutionIdempotencyEntry IdempotencyEntry { get; }

    public static MobileMoneyPayoutExecutionResolution Execute(
        MobileMoneyPayoutExecutionBinding binding,
        MobileMoneyPayoutExecutionIdempotencyEntry idempotencyEntry)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(idempotencyEntry);

        if (!idempotencyEntry.Matches(binding.Intent))
        {
            throw new ArgumentException(
                "Idempotency entry must match the execution intent.",
                nameof(idempotencyEntry));
        }

        return new(false, binding, idempotencyEntry);
    }

    public static MobileMoneyPayoutExecutionResolution Replay(
        MobileMoneyPayoutExecutionIdempotencyEntry idempotencyEntry)
    {
        ArgumentNullException.ThrowIfNull(idempotencyEntry);
        return new(true, null, idempotencyEntry);
    }
}
