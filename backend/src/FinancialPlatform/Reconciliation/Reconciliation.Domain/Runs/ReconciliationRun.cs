using Reconciliation.Domain.Exceptions;
using Reconciliation.Domain.Matches;

namespace Reconciliation.Domain.Runs;

public sealed class ReconciliationRun
{
    private readonly List<ReconciliationMatch> _matches = [];
    private readonly List<ReconciliationException> _exceptions = [];

    public ReconciliationRun(
        Guid runId,
        string partnerId,
        DateTime periodStartUtc,
        DateTime periodEndUtc)
    {
        if (runId == Guid.Empty)
            throw new ArgumentException("Run ID is required.");

        RunId = runId;
        PartnerId = partnerId;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
    }

    public Guid RunId { get; }

    public string PartnerId { get; }

    public DateTime PeriodStartUtc { get; }

    public DateTime PeriodEndUtc { get; }

    public ReconciliationRunStatus Status { get; private set; }
        = ReconciliationRunStatus.Created;

    public IReadOnlyCollection<ReconciliationMatch> Matches =>
        _matches.AsReadOnly();

    public IReadOnlyCollection<ReconciliationException> Exceptions =>
        _exceptions.AsReadOnly();

    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; private set; }

    public void Start()
    {
        if (Status != ReconciliationRunStatus.Created)
            throw new InvalidOperationException(
                "Only created runs can be started.");

        Status = ReconciliationRunStatus.Running;
    }

    public void AddMatch(ReconciliationMatch match)
    {
        if (Status != ReconciliationRunStatus.Running)
            throw new InvalidOperationException(
                "Matches can only be added to a running reconciliation.");

        _matches.Add(match);
    }

    public void AddException(ReconciliationException exception)
    {
        if (Status != ReconciliationRunStatus.Running)
            throw new InvalidOperationException(
                "Exceptions can only be added to a running reconciliation.");

        _exceptions.Add(exception);
    }

    public void Complete()
    {
        if (Status != ReconciliationRunStatus.Running)
            throw new InvalidOperationException(
                "Only running reconciliations can complete.");

        Status = ReconciliationRunStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }
}

public enum ReconciliationRunStatus
{
    Created,
    Running,
    Completed,
    Failed
}
