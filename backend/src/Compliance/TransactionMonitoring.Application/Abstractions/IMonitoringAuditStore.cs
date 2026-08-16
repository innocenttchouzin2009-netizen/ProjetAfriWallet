namespace AfriWallet.Compliance.TransactionMonitoring.Application.Abstractions;

public sealed record MonitoringAuditEvent(
    Guid Id,
    Guid TransactionId,
    string Awid,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IMonitoringAuditStore
{
    Task AppendAsync(
        MonitoringAuditEvent auditEvent,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MonitoringAuditEvent>> GetByTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);
}