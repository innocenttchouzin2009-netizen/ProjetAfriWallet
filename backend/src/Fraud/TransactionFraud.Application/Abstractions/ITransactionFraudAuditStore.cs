namespace AfriWallet.Fraud.TransactionFraud.Application.Abstractions;

public sealed record TransactionFraudAuditEvent(
    Guid Id,
    Guid TransactionId,
    Guid DetectionId,
    string Awid,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface ITransactionFraudAuditStore
{
    Task AppendAsync(TransactionFraudAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TransactionFraudAuditEvent>> GetByDetectionAsync(Guid detectionId, CancellationToken cancellationToken = default);
}
