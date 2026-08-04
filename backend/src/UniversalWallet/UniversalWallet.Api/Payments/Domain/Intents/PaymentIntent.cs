namespace UniversalWallet.Api.Payments.Domain.Intents;

public enum PaymentIntentStatus
{
    Created,
    Validated,
    Authorized,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Expired
}

public enum RecipientType
{
    Wallet,
    Awid,
    MobileMoney,
    BankAccount,
    Merchant,
    Qr
}

public enum PaymentPurpose
{
    FamilySupport,
    Salary,
    Business,
    Shopping,
    Savings,
    Education,
    Healthcare,
    Donation,
    Rent,
    Bill,
    Africircle,
    Other
}

public readonly record struct PaymentAmount(long MinorAmount, string CurrencyCode)
{
    public static PaymentAmount Create(long minorAmount, string currencyCode)
    {
        if (minorAmount <= 0)
        {
            throw new ArgumentException("PAYMENT_AMOUNT_INVALID");
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("CURRENCY_REQUIRED");
        }

        return new PaymentAmount(minorAmount, currencyCode.Trim().ToUpperInvariant());
    }
}

public sealed class PaymentIntent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid PayerAwid { get; init; }
    public Guid SourceWalletId { get; init; }
    public RecipientType RecipientType { get; init; }
    public string RecipientReference { get; init; } = string.Empty;
    public Guid? DestinationWalletId { get; init; }
    public long AmountMinor { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public PaymentPurpose Purpose { get; init; }
    public string Description { get; init; } = string.Empty;
    public PaymentIntentStatus Status { get; set; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public Guid? ClientReference { get; init; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CancelledAt { get; set; }
    public string? FailureCode { get; set; }
    public string PayloadHash { get; init; } = string.Empty;
    public int Version { get; set; } = 1;

    public bool IsTerminal => Status is PaymentIntentStatus.Completed or PaymentIntentStatus.Failed or PaymentIntentStatus.Cancelled or PaymentIntentStatus.Expired;

    public void Cancel(string? failureCode = null)
    {
        if (IsTerminal)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_ALREADY_TERMINAL");
        }

        if (Status == PaymentIntentStatus.Expired)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_EXPIRED");
        }

        Status = PaymentIntentStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
        FailureCode = failureCode;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version += 1;
    }

    public void MarkExpired()
    {
        if (IsTerminal)
        {
            return;
        }

        Status = PaymentIntentStatus.Expired;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version += 1;
    }
}
