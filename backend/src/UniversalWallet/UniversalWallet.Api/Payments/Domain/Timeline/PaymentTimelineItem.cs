namespace UniversalWallet.Api.Payments.Domain.Timeline;

public enum PaymentTimelineDirection
{
    Incoming,
    Outgoing,
    Internal
}

public enum PaymentTimelineType
{
    WalletTransfer,
    QrPayment,
    MerchantPayment,
    PaymentRequest,
    MobileMoney,
    BankTransfer,
    FxConversion,
    Refund,
    Reversal
}

public enum PaymentTimelineStatus
{
    Pending,
    Authorized,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Expired,
    Reversed,
    Refunded
}

public sealed class PaymentTimelineItem
{
    public Guid TimelineId { get; init; } = Guid.CreateVersion7();
    public Guid OwnerAwidId { get; init; }
    public Guid PaymentIntentId { get; init; }
    public Guid? TransferId { get; init; }
    public Guid? SettlementId { get; init; }
    public PaymentTimelineDirection Direction { get; set; }
    public PaymentTimelineType Type { get; set; } = PaymentTimelineType.WalletTransfer;
    public PaymentTimelineStatus Status { get; set; } = PaymentTimelineStatus.Pending;
    public long AmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string CounterpartyDisplayName { get; set; } = string.Empty;
    public string CounterpartyAlias { get; set; } = string.Empty;
    public string CounterpartyPublicAwid { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PublicReference { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool ReceiptAvailable { get; set; }
    public int ProjectionVersion { get; set; } = 1;
}
