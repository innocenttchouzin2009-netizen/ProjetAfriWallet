namespace MobileMoney.Production.Audit;

public sealed class AuditContext
{
    public string? CorrelationId { get; init; }
    public string? TraceId { get; init; }
    public string? TransactionId { get; init; }
    public string? ProviderReference { get; init; }
    public string? AwidId { get; init; }
    public string? WalletId { get; init; }
    public string? ProviderCode { get; init; }
    public string? OperationType { get; init; }
    public string? ActorType { get; init; }
    public string? ActorId { get; init; }
    public string? Environment { get; init; }
    public string? IpAddress { get; init; }
    public string? DeviceId { get; init; }
    public string? PhoneNumber { get; init; }
    public long DurationMs { get; init; }
    public string? PreviousAuditHash { get; init; }
}
