namespace AfriWallet.PaymentPlatform.MobileMoney.Domain;

public sealed class MobileMoneyPayment
{
    public Guid Id { get; init; }

    public required string PaymentIntentId { get; init; }

    public required string ProviderCode { get; init; }

    public required string Country { get; init; }

    public required string Currency { get; init; }

    public required string Msisdn { get; init; }

    public decimal Amount { get; init; }

    public required string IdempotencyKey { get; init; }

    public string? ProviderReference { get; set; }

    public MobileMoneyPaymentStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}