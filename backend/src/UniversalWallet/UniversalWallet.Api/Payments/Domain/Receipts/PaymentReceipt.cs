namespace UniversalWallet.Api.Payments.Domain.Receipts;

public enum PaymentReceiptStatus
{
    Valid,
    Revoked,
    Superseded
}

public sealed class PaymentReceipt
{
    public Guid ReceiptId { get; init; } = Guid.CreateVersion7();
    public Guid PaymentIntentId { get; init; }
    public Guid? TransferId { get; init; }
    public Guid? SettlementId { get; init; }
    public string PublicReference { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public string SenderDisplay { get; set; } = string.Empty;
    public string RecipientDisplay { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public long FeeMinor { get; set; }
    public decimal FxRate { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public PaymentReceiptStatus Status { get; set; } = PaymentReceiptStatus.Valid;
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? SettledAt { get; set; }
    public string VerificationTokenHash { get; set; } = string.Empty;
    public int DocumentVersion { get; set; } = 1;
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public string Signature { get; set; } = string.Empty;
}
