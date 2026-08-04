namespace UniversalWallet.Api.Payments.Domain.MerchantPayments;

public enum MerchantPaymentRequestStatus
{
    Created,
    Active,
    Processing,
    Paid,
    Expired,
    Cancelled,
    Failed
}

public sealed class MerchantPaymentRequest
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid MerchantId { get; init; }
    public Guid MerchantWalletId { get; init; }
    public Guid? QrTokenId { get; init; }
    public long AmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public MerchantPaymentRequestStatus Status { get; set; } = MerchantPaymentRequestStatus.Created;
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddMinutes(15);
    public int MaxUses { get; set; } = 1;
    public int UseCount { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public int Version { get; set; } = 1;
}
