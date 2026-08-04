namespace Observability.Api.Domain;

public sealed class StructuredLogEntry
{
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("o");
    public string Service { get; init; } = "UniversalWallet";
    public string Event { get; init; } = string.Empty;
    public string? PaymentIntentId { get; init; }
    public string? WalletId { get; init; }
    public string? Awid { get; init; }
    public int? DurationMs { get; init; }
    public string? CorrelationId { get; init; }
    public string? Result { get; init; }
}

public sealed class AuditEvent
{
    public string Code { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
