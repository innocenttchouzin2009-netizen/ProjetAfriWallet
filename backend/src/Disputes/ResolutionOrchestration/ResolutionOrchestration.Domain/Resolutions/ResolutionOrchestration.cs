using AfriWallet.Disputes.Resolution.Domain.Compensation;

namespace AfriWallet.Disputes.Resolution.Domain.Resolutions;

/// Orchestrates recovery/resolution against a sandbox provider only.
/// It never executes a real refund, chargeback, money movement, or ledger mutation.
public sealed class ResolutionOrchestration
{
    private readonly List<ResolutionAttempt> _attempts = new();
    private readonly List<CompensationRecord> _compensations = new();

    public ResolutionOrchestration(
        Guid resolutionId,
        Guid decisionId,
        Guid claimId,
        string awid,
        ResolutionRoute route,
        string idempotencyKey,
        DateTimeOffset createdAtUtc)
    {
        if (resolutionId == Guid.Empty)
            throw new ArgumentException("Resolution id is required.", nameof(resolutionId));
        if (decisionId == Guid.Empty)
            throw new ArgumentException("Decision id is required.", nameof(decisionId));
        if (claimId == Guid.Empty)
            throw new ArgumentException("Claim id is required.", nameof(claimId));
        if (string.IsNullOrWhiteSpace(awid))
            throw new ArgumentException("AWID is required.", nameof(awid));
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        ResolutionId = resolutionId;
        DecisionId = decisionId;
        ClaimId = claimId;
        Awid = awid.Trim();
        Route = route;
        IdempotencyKey = idempotencyKey.Trim();
        Status = ResolutionStatus.Created;
        ReasonCode = route == ResolutionRoute.Refund
            ? ResolutionReasonCode.RefundRouteSelected
            : ResolutionReasonCode.ChargebackRouteSelected;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid ResolutionId { get; }
    public Guid DecisionId { get; }
    public Guid ClaimId { get; }
    public string Awid { get; }
    public ResolutionRoute Route { get; }
    public string IdempotencyKey { get; }
    public ResolutionStatus Status { get; private set; }
    public ResolutionReasonCode ReasonCode { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? ProviderReference { get; private set; }
    public int AttemptCount => _attempts.Count;
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public IReadOnlyCollection<ResolutionAttempt> Attempts => _attempts.AsReadOnly();
    public IReadOnlyCollection<CompensationRecord> Compensations => _compensations.AsReadOnly();

    public void MarkDispatchPending(string correlationId, DateTimeOffset now)
    {
        EnsureMutable();
        if (Status is not (ResolutionStatus.Created or ResolutionStatus.RetryPending))
            throw new InvalidOperationException("Resolution must be created or pending retry to dispatch.");
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation id is required.", nameof(correlationId));

        CorrelationId = correlationId.Trim();
        Status = ResolutionStatus.DispatchPending;
        UpdatedAtUtc = now;
    }

    public void RecordAttempt(string? providerReference, string result, DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != ResolutionStatus.DispatchPending)
            throw new InvalidOperationException("Resolution must be dispatch-pending to record an attempt.");
        if (string.IsNullOrWhiteSpace(CorrelationId))
            throw new InvalidOperationException("Correlation id must be set before recording an attempt.");

        _attempts.Add(new ResolutionAttempt(Guid.NewGuid(), _attempts.Count + 1, CorrelationId, providerReference, result, now));
        ProviderReference = providerReference ?? ProviderReference;
        Status = ResolutionStatus.Dispatched;
        UpdatedAtUtc = now;
    }

    public void Acknowledge(string? providerReference, DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != ResolutionStatus.Dispatched)
            throw new InvalidOperationException("Resolution must be dispatched before acknowledgement.");

        ProviderReference = providerReference ?? ProviderReference;
        Status = ResolutionStatus.Acknowledged;
        ReasonCode = ResolutionReasonCode.ProviderAcknowledged;
        UpdatedAtUtc = now;
    }

    public void ScheduleRetry(ResolutionReasonCode reason, DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != ResolutionStatus.Dispatched)
            throw new InvalidOperationException("Resolution must be dispatched before scheduling a retry.");

        Status = ResolutionStatus.RetryPending;
        ReasonCode = reason;
        UpdatedAtUtc = now;
    }

    public void RequireCompensation(string reason, string? providerReference, DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != ResolutionStatus.Dispatched)
            throw new InvalidOperationException("Resolution must be dispatched before requiring compensation.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Compensation reason is required.", nameof(reason));

        ProviderReference = providerReference ?? ProviderReference;
        _compensations.Add(new CompensationRecord(Guid.NewGuid(), reason.Trim(), providerReference, now, null));
        Status = ResolutionStatus.CompensationRequired;
        ReasonCode = ResolutionReasonCode.CompensationRequired;
        UpdatedAtUtc = now;
    }

    public void CompleteCompensation(DateTimeOffset now)
    {
        if (Status != ResolutionStatus.CompensationRequired)
            throw new InvalidOperationException("Compensation is not required.");

        var last = _compensations.LastOrDefault()
            ?? throw new InvalidOperationException("Compensation record missing.");
        var index = _compensations.FindIndex(x => x.CompensationId == last.CompensationId);
        _compensations[index] = last with { CompletedAtUtc = now };

        Status = ResolutionStatus.Compensated;
        ReasonCode = ResolutionReasonCode.CompensationCompleted;
        UpdatedAtUtc = now;
    }

    public void RequireManualIntervention(ResolutionReasonCode reason, DateTimeOffset now)
    {
        EnsureMutable();
        Status = ResolutionStatus.ManualInterventionRequired;
        ReasonCode = reason;
        UpdatedAtUtc = now;
    }

    public void Fail(ResolutionReasonCode reason, DateTimeOffset now)
    {
        EnsureMutable();
        Status = ResolutionStatus.Failed;
        ReasonCode = reason;
        UpdatedAtUtc = now;
    }

    public void Resolve(DateTimeOffset now)
    {
        EnsureMutable();
        if (Status is not (ResolutionStatus.Acknowledged or ResolutionStatus.Compensated))
            throw new InvalidOperationException("Resolution cannot complete from current state.");

        Status = ResolutionStatus.Resolved;
        ReasonCode = ResolutionReasonCode.ProviderCompleted;
        ResolvedAtUtc = now;
        UpdatedAtUtc = now;
    }

    private void EnsureMutable()
    {
        if (Status is ResolutionStatus.Resolved or ResolutionStatus.Failed or ResolutionStatus.ManualInterventionRequired)
            throw new InvalidOperationException("Terminal resolution state is immutable.");
    }
}
