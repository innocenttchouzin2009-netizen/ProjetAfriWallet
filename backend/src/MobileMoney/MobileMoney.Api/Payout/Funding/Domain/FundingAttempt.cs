namespace MobileMoney.Production.Payout.Funding.Domain;

public sealed record FundingAttempt
{
    public Guid Id { get; }
    public Guid CorrelationId { get; }
    public FundingAllocation Allocation { get; }
    public string ExecutionIdempotencyKey { get; }
    public FundingAttemptStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ProcessingStartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? ProviderReference { get; private set; }
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
        ExecutionIdempotencyKey = BuildExecutionIdempotencyKey(
            correlationId,
            id);
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

        ProcessingStartedAtUtc = occurredAtUtc;
    }

    public void MarkSucceeded(
        DateTimeOffset occurredAtUtc,
        string? providerReference = null)
    {
        var normalizedProviderReference =
            NormalizeOptionalProviderReference(providerReference);

        TransitionTo(
            FundingAttemptStatus.Succeeded,
            occurredAtUtc,
            reason: null,
            FundingAttemptStatus.Processing);

        ProviderReference = normalizedProviderReference;
        CompletedAtUtc = occurredAtUtc;
    }

    public void MarkFailed(
        DateTimeOffset occurredAtUtc,
        string reason,
        string? providerReference = null)
    {
        var normalizedProviderReference =
            NormalizeOptionalProviderReference(providerReference);

        TransitionTo(
            FundingAttemptStatus.Failed,
            occurredAtUtc,
            NormalizeReason(reason),
            FundingAttemptStatus.Processing);

        ProviderReference = normalizedProviderReference;
        CompletedAtUtc = occurredAtUtc;
    }

    public void Cancel(DateTimeOffset occurredAtUtc, string reason)
    {
        TransitionTo(
            FundingAttemptStatus.Cancelled,
            occurredAtUtc,
            NormalizeReason(reason),
            FundingAttemptStatus.Planned,
            FundingAttemptStatus.Processing);

        CompletedAtUtc = occurredAtUtc;
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

    private static string BuildExecutionIdempotencyKey(
        Guid correlationId,
        Guid attemptId) =>
        $"momo-payout-funding:{correlationId:N}:{attemptId:N}";

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

    private static string? NormalizeOptionalProviderReference(
        string? providerReference)
    {
        if (providerReference is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(providerReference))
        {
            throw new ArgumentException(
                "Provider reference cannot be blank when supplied.",
                nameof(providerReference));
        }

        return providerReference.Trim();
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
