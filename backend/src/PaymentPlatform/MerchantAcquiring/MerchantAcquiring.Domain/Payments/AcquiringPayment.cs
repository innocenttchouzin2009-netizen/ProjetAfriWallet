namespace MerchantAcquiring.Domain.Payments;

public sealed class AcquiringPayment
{
    public AcquiringPayment(
        Guid paymentId,
        Guid paymentIntentId,
        string merchantId,
        string currencyCode,
        long amountMinor,
        string idempotencyKey)
    {
        if (paymentId == Guid.Empty ||
            paymentIntentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment IDs are required.");
        }

        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amountMinor));

        PaymentId = paymentId;
        PaymentIntentId = paymentIntentId;
        MerchantId = Require(merchantId);
        CurrencyCode =
            currencyCode.Trim().ToUpperInvariant();
        AmountMinor = amountMinor;
        IdempotencyKey =
            Require(idempotencyKey);
    }

    public Guid PaymentId { get; }

    public Guid PaymentIntentId { get; }

    public string MerchantId { get; }

    public string CurrencyCode { get; }

    public long AmountMinor { get; }

    public long FeeMinor { get; private set; }

    public long NetSettlementMinor =>
        AmountMinor - FeeMinor;

    public string IdempotencyKey { get; }

    public string? ProviderId { get; private set; }

    public string? ProviderReference { get; private set; }

    public AcquiringPaymentStatus Status { get; private set; }
        = AcquiringPaymentStatus.Created;

    public DateTime CreatedAtUtc { get; } =
        DateTime.UtcNow;

    public DateTime? AuthorizedAtUtc { get; private set; }

    public DateTime? CapturedAtUtc { get; private set; }

    public void ApplyFee(long feeMinor)
    {
        if (Status != AcquiringPaymentStatus.Created)
            throw new InvalidOperationException(
                "Fee must be calculated before processing.");

        if (feeMinor < 0 ||
            feeMinor >= AmountMinor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(feeMinor));
        }

        FeeMinor = feeMinor;
    }

    public void Route(
        string providerId)
    {
        if (Status != AcquiringPaymentStatus.Created)
            throw new InvalidOperationException(
                "Payment cannot be routed.");

        ProviderId = Require(providerId);
        Status = AcquiringPaymentStatus.Routed;
    }

    public void Authorize(
        string providerReference)
    {
        if (Status != AcquiringPaymentStatus.Routed)
            throw new InvalidOperationException(
                "Only routed payments can be authorized.");

        ProviderReference =
            Require(providerReference);

        Status =
            AcquiringPaymentStatus.Authorized;

        AuthorizedAtUtc =
            DateTime.UtcNow;
    }

    public void Capture()
    {
        if (Status !=
            AcquiringPaymentStatus.Authorized)
        {
            throw new InvalidOperationException(
                "Only authorized payments can be captured.");
        }

        Status =
            AcquiringPaymentStatus.Captured;

        CapturedAtUtc =
            DateTime.UtcNow;
    }

    public void Fail()
    {
        if (Status ==
            AcquiringPaymentStatus.Captured)
        {
            throw new InvalidOperationException(
                "Captured payment cannot fail.");
        }

        Status =
            AcquiringPaymentStatus.Failed;
    }

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Value is required.");

        return value.Trim();
    }
}

public enum AcquiringPaymentStatus
{
    Created,
    Routed,
    Authorized,
    Captured,
    Failed,
    Cancelled,
    Refunded,
    PartiallyRefunded
}
