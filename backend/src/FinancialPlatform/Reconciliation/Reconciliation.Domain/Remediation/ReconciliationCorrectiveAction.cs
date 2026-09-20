namespace Reconciliation.Domain.Remediation;

public enum ReconciliationCorrectiveActionStatus
{
    Pending = 1,
    Completed = 2,
    Cancelled = 3
}

public sealed class ReconciliationCorrectiveAction
{
    private ReconciliationCorrectiveAction(
        Guid actionId,
        Guid resolutionId,
        Guid reviewId,
        string partnerId,
        string? internalRecordId,
        string? externalRecordId,
        string actionCode,
        string description,
        string createdBy,
        DateTime createdAtUtc)
    {
        ActionId = actionId;
        ResolutionId = resolutionId;
        ReviewId = reviewId;
        PartnerId = partnerId;
        InternalRecordId = internalRecordId;
        ExternalRecordId = externalRecordId;
        ActionCode = actionCode;
        Description = description;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
        Status = ReconciliationCorrectiveActionStatus.Pending;
    }

    public Guid ActionId { get; }
    public Guid ResolutionId { get; }
    public Guid ReviewId { get; }
    public string PartnerId { get; }
    public string? InternalRecordId { get; }
    public string? ExternalRecordId { get; }
    public string ActionCode { get; }
    public string Description { get; }
    public string CreatedBy { get; }
    public DateTime CreatedAtUtc { get; }
    public ReconciliationCorrectiveActionStatus Status { get; private set; }
    public string? CompletedBy { get; private set; }
    public string? CompletionEvidenceReference { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public static ReconciliationCorrectiveAction Create(
        Guid resolutionId,
        Guid reviewId,
        string partnerId,
        string? internalRecordId,
        string? externalRecordId,
        string actionCode,
        string description,
        string createdBy,
        DateTime createdAtUtc)
    {
        ValidateIdentity(resolutionId, reviewId, partnerId, internalRecordId, externalRecordId, description, createdBy, createdAtUtc);

        return new ReconciliationCorrectiveAction(
            Guid.NewGuid(),
            resolutionId,
            reviewId,
            partnerId.Trim(),
            Normalize(internalRecordId),
            Normalize(externalRecordId),
            NormalizeActionCode(actionCode),
            description.Trim(),
            createdBy.Trim(),
            createdAtUtc);
    }

    public static ReconciliationCorrectiveAction Restore(
        Guid actionId,
        Guid resolutionId,
        Guid reviewId,
        string partnerId,
        string? internalRecordId,
        string? externalRecordId,
        string actionCode,
        string description,
        string createdBy,
        DateTime createdAtUtc,
        ReconciliationCorrectiveActionStatus status,
        string? completedBy,
        string? completionEvidenceReference,
        DateTime? completedAtUtc,
        string? cancelledBy,
        string? cancellationReason,
        DateTime? cancelledAtUtc)
    {
        if (actionId == Guid.Empty)
            throw new ArgumentException("Corrective action id is required.", nameof(actionId));

        ValidateIdentity(resolutionId, reviewId, partnerId, internalRecordId, externalRecordId, description, createdBy, createdAtUtc);

        var action = new ReconciliationCorrectiveAction(
            actionId,
            resolutionId,
            reviewId,
            partnerId.Trim(),
            Normalize(internalRecordId),
            Normalize(externalRecordId),
            NormalizeActionCode(actionCode),
            description.Trim(),
            createdBy.Trim(),
            createdAtUtc);

        switch (status)
        {
            case ReconciliationCorrectiveActionStatus.Pending:
                if (completedBy is not null || completionEvidenceReference is not null || completedAtUtc is not null ||
                    cancelledBy is not null || cancellationReason is not null || cancelledAtUtc is not null)
                    throw new InvalidOperationException("Pending corrective action cannot contain terminal lifecycle evidence.");
                break;

            case ReconciliationCorrectiveActionStatus.Completed:
                if (string.IsNullOrWhiteSpace(completedBy) ||
                    string.IsNullOrWhiteSpace(completionEvidenceReference) ||
                    completedAtUtc is null)
                    throw new InvalidOperationException("Completed corrective action requires completion actor, evidence and timestamp.");
                if (cancelledBy is not null || cancellationReason is not null || cancelledAtUtc is not null)
                    throw new InvalidOperationException("Completed corrective action cannot contain cancellation evidence.");
                action.Complete(completedBy, completionEvidenceReference, EnsureUtcValue(completedAtUtc.Value, nameof(completedAtUtc)));
                break;

            case ReconciliationCorrectiveActionStatus.Cancelled:
                if (string.IsNullOrWhiteSpace(cancelledBy) ||
                    string.IsNullOrWhiteSpace(cancellationReason) ||
                    cancelledAtUtc is null)
                    throw new InvalidOperationException("Cancelled corrective action requires cancellation actor, reason and timestamp.");
                if (completedBy is not null || completionEvidenceReference is not null || completedAtUtc is not null)
                    throw new InvalidOperationException("Cancelled corrective action cannot contain completion evidence.");
                action.Cancel(cancelledBy, cancellationReason, EnsureUtcValue(cancelledAtUtc.Value, nameof(cancelledAtUtc)));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported corrective action status.");
        }

        return action;
    }

    public void Complete(string completedBy, string evidenceReference, DateTime completedAtUtc)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(completedBy))
            throw new ArgumentException("Completer id is required.", nameof(completedBy));
        if (string.IsNullOrWhiteSpace(evidenceReference))
            throw new ArgumentException("Completion evidence reference is required.", nameof(evidenceReference));
        EnsureTransitionTime(completedAtUtc, nameof(completedAtUtc));

        Status = ReconciliationCorrectiveActionStatus.Completed;
        CompletedBy = completedBy.Trim();
        CompletionEvidenceReference = evidenceReference.Trim();
        CompletedAtUtc = completedAtUtc;
    }

    public void Cancel(string cancelledBy, string reason, DateTime cancelledAtUtc)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(cancelledBy))
            throw new ArgumentException("Canceller id is required.", nameof(cancelledBy));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));
        EnsureTransitionTime(cancelledAtUtc, nameof(cancelledAtUtc));

        Status = ReconciliationCorrectiveActionStatus.Cancelled;
        CancelledBy = cancelledBy.Trim();
        CancellationReason = reason.Trim();
        CancelledAtUtc = cancelledAtUtc;
    }

    private static void ValidateIdentity(
        Guid resolutionId,
        Guid reviewId,
        string partnerId,
        string? internalRecordId,
        string? externalRecordId,
        string description,
        string createdBy,
        DateTime createdAtUtc)
    {
        if (resolutionId == Guid.Empty)
            throw new ArgumentException("Resolution id is required.", nameof(resolutionId));
        if (reviewId == Guid.Empty)
            throw new ArgumentException("Review id is required.", nameof(reviewId));
        if (string.IsNullOrWhiteSpace(partnerId))
            throw new ArgumentException("Partner id is required.", nameof(partnerId));
        if (string.IsNullOrWhiteSpace(internalRecordId) && string.IsNullOrWhiteSpace(externalRecordId))
            throw new ArgumentException("At least one reconciliation record id is required.");
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Corrective action description is required.", nameof(description));
        if (description.Trim().Length > 500)
            throw new ArgumentOutOfRangeException(nameof(description), "Corrective action description cannot exceed 500 characters.");
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Corrective action creator id is required.", nameof(createdBy));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
    }

    private void EnsurePending()
    {
        if (Status != ReconciliationCorrectiveActionStatus.Pending)
            throw new InvalidOperationException("Corrective action is already terminal.");
    }

    private void EnsureTransitionTime(DateTime atUtc, string parameterName)
    {
        EnsureUtc(atUtc, parameterName);
        if (atUtc < CreatedAtUtc)
            throw new ArgumentException("Corrective action transition cannot precede creation.", parameterName);
    }

    private static string NormalizeActionCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Corrective action code is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > 64)
            throw new ArgumentOutOfRangeException(nameof(value), "Corrective action code cannot exceed 64 characters.");

        if (normalized.Any(ch => !(char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.')))
            throw new ArgumentException("Corrective action code contains unsupported characters.", nameof(value));

        return normalized;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime EnsureUtcValue(DateTime value, string parameterName)
    {
        EnsureUtc(value, parameterName);
        return value;
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Corrective action timestamp must be UTC.", parameterName);
    }
}
