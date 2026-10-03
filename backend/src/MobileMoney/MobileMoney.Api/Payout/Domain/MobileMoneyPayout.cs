namespace MobileMoney.Production.Payout.Domain;

public sealed class MobileMoneyPayout
{
    private MobileMoneyPayout(
        Guid payoutId,
        string sourceWalletId,
        long amountMinor,
        string currency,
        MobileMoneyBeneficiary beneficiary,
        string idempotencyKey,
        MobileMoneyPayoutStatus status,
        string? providerReference,
        string? failureCode,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        PayoutId = payoutId;
        SourceWalletId = sourceWalletId;
        AmountMinor = amountMinor;
        Currency = currency;
        Beneficiary = beneficiary;
        IdempotencyKey = idempotencyKey;
        Status = status;
        ProviderReference = providerReference;
        FailureCode = failureCode;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid PayoutId { get; }
    public string SourceWalletId { get; }
    public long AmountMinor { get; }
    public string Currency { get; }
    public MobileMoneyBeneficiary Beneficiary { get; }
    public string IdempotencyKey { get; }
    public MobileMoneyPayoutStatus Status { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? FailureCode { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static MobileMoneyPayout Create(
        string sourceWalletId,
        long amountMinor,
        string currency,
        MobileMoneyBeneficiary beneficiary,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(sourceWalletId))
            throw new ArgumentException("Source wallet id is required.", nameof(sourceWalletId));
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));
        if (beneficiary is null)
            throw new ArgumentNullException(nameof(beneficiary));
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        var normalizedCurrency = NormalizeCurrency(currency);
        EnsureUtc(now);

        return new MobileMoneyPayout(
            Guid.NewGuid(),
            sourceWalletId.Trim(),
            amountMinor,
            normalizedCurrency,
            beneficiary,
            idempotencyKey.Trim(),
            MobileMoneyPayoutStatus.Created,
            null,
            null,
            now,
            now);
    }

    public static MobileMoneyPayout Restore(
        Guid payoutId,
        string sourceWalletId,
        long amountMinor,
        string currency,
        MobileMoneyBeneficiary beneficiary,
        string idempotencyKey,
        MobileMoneyPayoutStatus status,
        string? providerReference,
        string? failureCode,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (payoutId == Guid.Empty)
            throw new ArgumentException("Payout id is required.", nameof(payoutId));
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status));
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (updatedAtUtc < createdAtUtc)
            throw new ArgumentException(
                "Updated timestamp cannot precede creation.",
                nameof(updatedAtUtc));

        var created = Create(
            sourceWalletId,
            amountMinor,
            currency,
            beneficiary,
            idempotencyKey,
            createdAtUtc);

        return new MobileMoneyPayout(
            payoutId,
            created.SourceWalletId,
            created.AmountMinor,
            created.Currency,
            created.Beneficiary,
            created.IdempotencyKey,
            status,
            string.IsNullOrWhiteSpace(providerReference) ? null : providerReference.Trim(),
            string.IsNullOrWhiteSpace(failureCode) ? null : failureCode.Trim(),
            createdAtUtc,
            updatedAtUtc);
    }

    public void Start(DateTimeOffset now)
    {
        EnsureCanTransitionFrom(MobileMoneyPayoutStatus.Created);
        AdvanceTime(now);
        Status = MobileMoneyPayoutStatus.Processing;
    }

    public void MarkSubmitted(string providerReference, DateTimeOffset now)
    {
        EnsureCanTransitionFrom(MobileMoneyPayoutStatus.Processing);
        if (string.IsNullOrWhiteSpace(providerReference))
            throw new ArgumentException(
                "Provider reference is required.",
                nameof(providerReference));

        AdvanceTime(now);
        ProviderReference = providerReference.Trim();
        FailureCode = null;
        Status = MobileMoneyPayoutStatus.Submitted;
    }

    public void Succeed(DateTimeOffset now)
    {
        EnsureCanTransitionFrom(MobileMoneyPayoutStatus.Submitted);
        AdvanceTime(now);
        FailureCode = null;
        Status = MobileMoneyPayoutStatus.Succeeded;
    }

    public void Fail(string failureCode, DateTimeOffset now)
    {
        if (Status is MobileMoneyPayoutStatus.Succeeded
            or MobileMoneyPayoutStatus.Failed
            or MobileMoneyPayoutStatus.Cancelled)
        {
            throw new InvalidOperationException("Terminal payout is immutable.");
        }

        if (string.IsNullOrWhiteSpace(failureCode))
            throw new ArgumentException("Failure code is required.", nameof(failureCode));

        AdvanceTime(now);
        FailureCode = failureCode.Trim();
        Status = MobileMoneyPayoutStatus.Failed;
    }

    public void Cancel(DateTimeOffset now)
    {
        EnsureCanTransitionFrom(MobileMoneyPayoutStatus.Created);
        AdvanceTime(now);
        Status = MobileMoneyPayoutStatus.Cancelled;
    }

    private void EnsureCanTransitionFrom(MobileMoneyPayoutStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException(
                $"Payout must be {expected} before this transition.");
    }

    private void AdvanceTime(DateTimeOffset now)
    {
        EnsureUtc(now);
        if (now < UpdatedAtUtc)
            throw new ArgumentException("Timestamp cannot move backwards.", nameof(now));
        UpdatedAtUtc = now;
    }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw new ArgumentException(
                "Currency must contain three ISO-like letters.",
                nameof(value));

        return normalized;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.");
    }
}
