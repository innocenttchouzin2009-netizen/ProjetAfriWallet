namespace UniversalWallet.Api.Payments.Domain.Transfers;

public enum PaymentTransferStatus
{
    Created,
    PostingLedger,
    Projecting,
    Completed,
    Failed,
    RolledBack
}

public sealed class PaymentTransfer
{
    public Guid TransferId { get; init; } = Guid.CreateVersion7();
    public Guid PaymentIntentId { get; init; }
    public Guid AuthorizationId { get; init; }
    public Guid ReservationId { get; init; }
    public Guid SourceWalletId { get; init; }
    public Guid DestinationWalletId { get; init; }
    public long AmountMinor { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public PaymentTransferStatus Status { get; set; } = PaymentTransferStatus.Created;
    public Guid? LedgerTransactionId { get; set; }
    public DateTimeOffset? ExecutedAt { get; set; }
    public string CorrelationId { get; init; } = string.Empty;
    public int Version { get; set; } = 1;
}
