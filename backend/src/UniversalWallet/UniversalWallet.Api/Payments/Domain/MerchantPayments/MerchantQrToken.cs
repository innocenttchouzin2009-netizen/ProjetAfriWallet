namespace UniversalWallet.Api.Payments.Domain.MerchantPayments;

public enum MerchantQrType
{
    MerchantStatic,
    MerchantDynamic,
    MerchantInvoice
}

public sealed class MerchantQrToken
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid MerchantId { get; init; }
    public MerchantQrType Type { get; set; } = MerchantQrType.MerchantDynamic;
    public string Token { get; set; } = string.Empty;
    public Guid? PaymentRequestId { get; set; }
    public bool Revoked { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public int MaxUses { get; set; } = 1;
    public int UseCount { get; set; }
    public int Version { get; set; } = 1;
}
