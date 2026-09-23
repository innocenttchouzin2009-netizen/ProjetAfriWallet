namespace AfriWallet.Merchants.Payout.Domain;

public enum MerchantPayoutDestinationType
{
    AfWalWallet = 1,
    BankAccount = 2,
    MobileMoney = 3
}

public enum MerchantPayoutStatus
{
    Created = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3
}

public sealed class MerchantPayoutDestination
{
    private MerchantPayoutDestination(
        Guid destinationId,
        string merchantId,
        MerchantPayoutDestinationType type,
        string reference,
        string currency,
        bool active,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        DestinationId = destinationId;
        MerchantId = merchantId;
        Type = type;
        Reference = reference;
        Currency = currency;
        Active = active;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid DestinationId { get; }
    public string MerchantId { get; }
    public MerchantPayoutDestinationType Type { get; }
    public string Reference { get; }
    public string Currency { get; }
    public bool Active { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static MerchantPayoutDestination Create(
        string merchantId,
        MerchantPayoutDestinationType type,
        string reference,
        string currency,
        DateTimeOffset now) =>
        Restore(Guid.NewGuid(), merchantId, type, reference, currency, true, now, now);

    public static MerchantPayoutDestination Restore(
        Guid destinationId,
        string merchantId,
        MerchantPayoutDestinationType type,
        string reference,
        string currency,
        bool active,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (destinationId == Guid.Empty) throw new ArgumentException("Destination id is required.", nameof(destinationId));
        if (string.IsNullOrWhiteSpace(merchantId)) throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("Destination reference is required.", nameof(reference));
        var normalizedCurrency = NormalizeCurrency(currency);
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (updatedAtUtc < createdAtUtc) throw new ArgumentException("Updated timestamp cannot precede creation.", nameof(updatedAtUtc));

        return new MerchantPayoutDestination(
            destinationId,
            merchantId.Trim(),
            type,
            reference.Trim(),
            normalizedCurrency,
            active,
            createdAtUtc,
            updatedAtUtc);
    }

    public void Disable(DateTimeOffset now)
    {
        EnsureUtc(now);
        if (now < UpdatedAtUtc) throw new ArgumentException("Timestamp cannot move backwards.", nameof(now));
        Active = false;
        UpdatedAtUtc = now;
    }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Currency is required.", nameof(value));
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must contain three ISO-like letters.", nameof(value));
        return normalized;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.");
    }
}

public sealed class MerchantPayoutExecution
{
    private MerchantPayoutExecution(
        Guid payoutId,
        Guid receivableId,
        string merchantId,
        long amountMinor,
        string currency,
        Guid destinationId,
        string idempotencyKey,
        MerchantPayoutStatus status,
        string? providerReference,
        string? failureCode,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        PayoutId = payoutId;
        ReceivableId = receivableId;
        MerchantId = merchantId;
        AmountMinor = amountMinor;
        Currency = currency;
        DestinationId = destinationId;
        IdempotencyKey = idempotencyKey;
        Status = status;
        ProviderReference = providerReference;
        FailureCode = failureCode;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid PayoutId { get; }
    public Guid ReceivableId { get; }
    public string MerchantId { get; }
    public long AmountMinor { get; }
    public string Currency { get; }
    public Guid DestinationId { get; }
    public string IdempotencyKey { get; }
    public MerchantPayoutStatus Status { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? FailureCode { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static MerchantPayoutExecution Create(
        Guid receivableId,
        string merchantId,
        long amountMinor,
        string currency,
        Guid destinationId,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (receivableId == Guid.Empty) throw new ArgumentException("Receivable id is required.", nameof(receivableId));
        if (destinationId == Guid.Empty) throw new ArgumentException("Destination id is required.", nameof(destinationId));
        if (string.IsNullOrWhiteSpace(merchantId)) throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        if (amountMinor <= 0) throw new ArgumentOutOfRangeException(nameof(amountMinor));
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        var normalizedCurrency = NormalizeCurrency(currency);
        EnsureUtc(now);

        return new MerchantPayoutExecution(
            Guid.NewGuid(),
            receivableId,
            merchantId.Trim(),
            amountMinor,
            normalizedCurrency,
            destinationId,
            idempotencyKey.Trim(),
            MerchantPayoutStatus.Created,
            null,
            null,
            now,
            now);
    }

    public static MerchantPayoutExecution Restore(
        Guid payoutId,
        Guid receivableId,
        string merchantId,
        long amountMinor,
        string currency,
        Guid destinationId,
        string idempotencyKey,
        MerchantPayoutStatus status,
        string? providerReference,
        string? failureCode,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (payoutId == Guid.Empty) throw new ArgumentException("Payout id is required.", nameof(payoutId));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        var created = Create(receivableId, merchantId, amountMinor, currency, destinationId, idempotencyKey, createdAtUtc);
        return new MerchantPayoutExecution(
            payoutId,
            created.ReceivableId,
            created.MerchantId,
            created.AmountMinor,
            created.Currency,
            created.DestinationId,
            created.IdempotencyKey,
            status,
            providerReference,
            failureCode,
            createdAtUtc,
            updatedAtUtc);
    }

    public void Start(DateTimeOffset now)
    {
        EnsureUtc(now);
        if (Status is MerchantPayoutStatus.Succeeded or MerchantPayoutStatus.Failed)
            throw new InvalidOperationException("Terminal payout execution is immutable.");
        Status = MerchantPayoutStatus.Processing;
        UpdatedAtUtc = now;
    }

    public void Complete(string providerReference, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (Status != MerchantPayoutStatus.Processing)
            throw new InvalidOperationException("Payout execution is not processing.");
        if (string.IsNullOrWhiteSpace(providerReference))
            throw new ArgumentException("Provider reference is required.", nameof(providerReference));

        ProviderReference = providerReference.Trim();
        FailureCode = null;
        Status = MerchantPayoutStatus.Succeeded;
        UpdatedAtUtc = now;
    }

    public void Fail(string failureCode, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (Status == MerchantPayoutStatus.Succeeded)
            throw new InvalidOperationException("Succeeded payout execution is immutable.");
        if (string.IsNullOrWhiteSpace(failureCode))
            throw new ArgumentException("Failure code is required.", nameof(failureCode));

        FailureCode = failureCode.Trim();
        Status = MerchantPayoutStatus.Failed;
        UpdatedAtUtc = now;
    }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Currency is required.", nameof(value));
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must contain three ISO-like letters.", nameof(value));
        return normalized;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.");
    }
}
