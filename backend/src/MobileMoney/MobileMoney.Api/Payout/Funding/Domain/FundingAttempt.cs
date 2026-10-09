namespace MobileMoney.Production.Payout.Funding.Domain;

public sealed record FundingAttempt
{
    public Guid Id { get; }
    public Guid CorrelationId { get; }
    public FundingAllocation Allocation { get; }
    public FundingAttemptStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string? StatusReason { get; private set; }

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
        UpdatedAtUtc = createdAtUtc;
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
        ValidateUtcTimestamp(createdAtUtc, nameof(createdAtUtc));

        return new FundingAttempt(
            id,
            correlationId,
            allocation,
            FundingAttemptStatus.Planned,
            createdAtUtc);
    }

    public void MarkProcessing(DateTimeOffset occurredAtUtc)
    {
        TransitionTo(
            FundingAttemptStatus.Processing,
            occurredAtUtc,
            reason: null,
            FundingAttemptStatus.Planned);
    }

    public void MarkSucceeded(DateTimeOffset occurredAtUtc)
    {
        TransitionTo(
            FundingAttemptStatus.Succeeded,
            occurredAtUtc,
            reason: null,
            FundingAttemptStatus.Processing);
    }

    public void MarkFailed(DateTimeOffset occurredAtUtc, string reason)
    {
        TransitionTo(
            FundingAttemptStatus.Failed,
            occurredAtUtc,
            NormalizeReason(reason),
            FundingAttemptStatus.Processing);
    }

    public void Cancel(DateTimeOffset occurredAtUtc, string reason)
    {
        TransitionTo(
            FundingAttemptStatus.Cancelled,
            occurredAtUtc,
            NormalizeReason(reason),
            FundingAttemptStatus.Planned,
            FundingAttemptStatus.Processing);
    }

    private void TransitionTo(
        FundingAttemptStatus nextStatus,
        DateTimeOffset occurredAtUtc,
        string? reason,
        params FundingAttemptStatus[] allowedCurrentStatuses)
    {
        ValidateUtcTimestamp(occurredAtUtc, nameof(occurredAtUtc));

        if (occurredAtUtc < UpdatedAtUtc)
        {
            throw new InvalidOperationException(
                "Funding attempt transitions cannot move backwards in time.");
        }

        if (!allowedCurrentStatuses.Contains(Status))
        {
            throw new InvalidOperationException(
                $"Funding attempt cannot transition from {Status} to {nextStatus}.");
        }

        Status = nextStatus;
        UpdatedAtUtc = occurredAtUtc;
        StatusReason = reason;
    }

    private static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "Funding attempt reason is required.",
                nameof(reason));
        }

        return reason.Trim();
    }

    private static void ValidateUtcTimestamp(
        DateTimeOffset timestamp,
        string parameterName)
    {
        if (timestamp.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Funding attempt timestamp must be UTC.",
                parameterName);
        }
    }
}
