namespace MobileMoney.Production.Payout.Funding.Domain;

public sealed record FundingAttempt
{
    public Guid Id { get; }
    public Guid CorrelationId { get; }
    public FundingAllocation Allocation { get; }
    public FundingAttemptStatus Status { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    private FundingAttempt(
        Guid id,
        Guid correlationId,
        FundingAllocation allocation,
        FundingAttemptStatus status,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        CorrelationId = correlationId;
        Allocation = allocation;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    public static FundingAttempt Create(
        Guid id,
        Guid correlationId,
        FundingAllocation allocation,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Funding attempt id cannot be empty.",
                nameof(id));
        }

        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Correlation id cannot be empty.",
                nameof(correlationId));
        }

        ArgumentNullException.ThrowIfNull(allocation);

        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Funding attempt timestamp must be UTC.",
                nameof(createdAtUtc));
        }

        return new FundingAttempt(
            id,
            correlationId,
            allocation,
            FundingAttemptStatus.Planned,
            createdAtUtc);
    }
}
