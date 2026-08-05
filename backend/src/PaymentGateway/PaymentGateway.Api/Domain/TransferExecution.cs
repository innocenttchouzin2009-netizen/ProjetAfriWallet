namespace PaymentGateway.Api.Domain;

public sealed class TransferExecution
{
    public TransferExecution(
        Guid id,
        Guid transferIntentId,
        string providerCode,
        string connectorType,
        string executionMode,
        string correlationId,
        string traceId,
        DateTimeOffset createdAt)
    {
        Id = id;
        TransferIntentId = transferIntentId;
        ProviderCode = providerCode;
        ConnectorType = connectorType;
        ExecutionMode = executionMode;
        CorrelationId = correlationId;
        TraceId = traceId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Status = ExecutionStatus.Queued;
        RetryCount = 0;
        Version = 1;
    }

    public Guid Id { get; }
    public Guid TransferIntentId { get; }
    public string ProviderCode { get; }
    public string ConnectorType { get; }
    public string ExecutionMode { get; }
    public ExecutionStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public long? DurationMs { get; private set; }
    public int RetryCount { get; private set; }
    public string? FailureReason { get; private set; }
    public string? ProviderReference { get; private set; }
    public string CorrelationId { get; }
    public string TraceId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int Version { get; private set; }

    public void Start()
    {
        if (Status is ExecutionStatus.Completed or ExecutionStatus.Failed or ExecutionStatus.Cancelled or ExecutionStatus.RolledBack)
        {
            throw new InvalidOperationException("Execution is already terminal.");
        }

        Status = ExecutionStatus.Dispatching;
        StartedAt = DateTimeOffset.UtcNow;
        UpdatedAt = StartedAt;
        Version += 1;
    }

    public void MarkSent(string providerReference)
    {
        EnsureActive();
        Status = ExecutionStatus.Sent;
        ProviderReference = providerReference;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version += 1;
    }

    public void MarkAccepted()
    {
        EnsureActive();
        Status = ExecutionStatus.Accepted;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version += 1;
    }

    public void MarkProcessing()
    {
        EnsureActive();
        Status = ExecutionStatus.Processing;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version += 1;
    }

    public void MarkSettled()
    {
        EnsureActive();
        Status = ExecutionStatus.Settled;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version += 1;
    }

    public void Complete()
    {
        EnsureActive();
        Status = ExecutionStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        DurationMs = Math.Max(1, (long)(CompletedAt.Value - StartedAt).TotalMilliseconds);
        UpdatedAt = CompletedAt.Value;
        Version += 1;
    }

    public void Fail(string reason)
    {
        if (Status is ExecutionStatus.Completed or ExecutionStatus.Cancelled or ExecutionStatus.RolledBack)
        {
            throw new InvalidOperationException("Cannot fail an already terminal execution.");
        }

        Status = ExecutionStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTimeOffset.UtcNow;
        DurationMs = Math.Max(1, (long)(CompletedAt.Value - StartedAt).TotalMilliseconds);
        UpdatedAt = CompletedAt.Value;
        Version += 1;
    }

    public void Cancel()
    {
        if (Status is ExecutionStatus.Completed or ExecutionStatus.Failed or ExecutionStatus.Cancelled or ExecutionStatus.RolledBack)
        {
            return;
        }

        Status = ExecutionStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
        DurationMs = Math.Max(1, (long)(CompletedAt.Value - StartedAt).TotalMilliseconds);
        UpdatedAt = CompletedAt.Value;
        Version += 1;
    }

    public void Rollback()
    {
        if (Status is ExecutionStatus.Completed or ExecutionStatus.Failed or ExecutionStatus.Cancelled or ExecutionStatus.RolledBack)
        {
            return;
        }

        Status = ExecutionStatus.RolledBack;
        CompletedAt = DateTimeOffset.UtcNow;
        DurationMs = Math.Max(1, (long)(CompletedAt.Value - StartedAt).TotalMilliseconds);
        UpdatedAt = CompletedAt.Value;
        Version += 1;
    }

    public void RecordRetry()
    {
        RetryCount += 1;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version += 1;
    }

    private void EnsureActive()
    {
        if (Status is ExecutionStatus.Completed or ExecutionStatus.Failed or ExecutionStatus.Cancelled or ExecutionStatus.RolledBack)
        {
            throw new InvalidOperationException("Execution is terminal.");
        }
    }
}
