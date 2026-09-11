namespace AfriWallet.PaymentRequests.Persistence;

public sealed class PaymentRequestEntity
{
    public Guid Id { get; set; }
    public Guid RequesterWalletId { get; set; }
    public int PayerReferenceKind { get; set; }
    public string PayerReferenceValue { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public Guid CorrelationId { get; set; }
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string? ExpiresAtUtc { get; set; }
    public string UpdatedAtUtc { get; set; } = string.Empty;
    public int Status { get; set; }
    public Guid? AcceptedPayerWalletId { get; set; }
    public string? AcceptedAtUtc { get; set; }
    public Guid? TransferId { get; set; }
    public string? ClosedAtUtc { get; set; }
}
