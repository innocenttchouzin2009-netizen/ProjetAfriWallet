namespace Reconciliation.Domain.Exceptions;

public sealed class ReconciliationException
{
    public ReconciliationException(
        Guid exceptionId,
        string code,
        string message,
        string? internalRecordId,
        string? externalRecordId)
    {
        ExceptionId = exceptionId;
        Code = code;
        Message = message;
        InternalRecordId = internalRecordId;
        ExternalRecordId = externalRecordId;
    }

    public Guid ExceptionId { get; }

    public string Code { get; }

    public string Message { get; }

    public string? InternalRecordId { get; }

    public string? ExternalRecordId { get; }

    public ReconciliationExceptionStatus Status { get; private set; }
        = ReconciliationExceptionStatus.Open;

    public DateTime CreatedAtUtc { get; }
        = DateTime.UtcNow;

    public void Resolve()
    {
        if (Status != ReconciliationExceptionStatus.Open)
            throw new InvalidOperationException(
                "Only open reconciliation exceptions can be resolved.");

        Status = ReconciliationExceptionStatus.Resolved;
    }
}

public enum ReconciliationExceptionStatus
{
    Open,
    Resolved,
    Ignored
}
