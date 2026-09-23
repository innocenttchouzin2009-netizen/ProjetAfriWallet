namespace AfriWallet.Merchants.Payout.Domain;

public enum MerchantPayoutProviderResultStatus
{
    Succeeded = 1,
    Failed = 2
}

public enum MerchantPayoutReconciliationStatus
{
    Matched = 1,
    Mismatch = 2,
    ManualReview = 3
}

public static class MerchantPayoutReconciliationReasonCode
{
    public const string Matched = "matched";
    public const string PayoutNotTerminal = "payout_not_terminal";
    public const string ProviderStatusMismatch = "provider_status_mismatch";
    public const string ProviderReferenceMismatch = "provider_reference_mismatch";
    public const string AmountMismatch = "amount_mismatch";
    public const string CurrencyMismatch = "currency_mismatch";
    public const string FailureCodeMismatch = "failure_code_mismatch";
}

public sealed record MerchantPayoutProviderResultRecord
{
    public Guid ResultId { get; }
    public Guid PayoutId { get; }
    public string MerchantId { get; }
    public MerchantPayoutProviderResultStatus Status { get; }
    public string? ProviderReference { get; }
    public string? FailureCode { get; }
    public long AmountMinor { get; }
    public string Currency { get; }
    public DateTimeOffset ObservedAtUtc { get; }

    private MerchantPayoutProviderResultRecord(
        Guid resultId,
        Guid payoutId,
        string merchantId,
        MerchantPayoutProviderResultStatus status,
        string? providerReference,
        string? failureCode,
        long amountMinor,
        string currency,
        DateTimeOffset observedAtUtc)
    {
        ResultId = resultId;
        PayoutId = payoutId;
        MerchantId = merchantId;
        Status = status;
        ProviderReference = providerReference;
        FailureCode = failureCode;
        AmountMinor = amountMinor;
        Currency = currency;
        ObservedAtUtc = observedAtUtc;
    }

    public static MerchantPayoutProviderResultRecord Create(
        Guid payoutId,
        string merchantId,
        MerchantPayoutProviderResultStatus status,
        string? providerReference,
        string? failureCode,
        long amountMinor,
        string currency,
        DateTimeOffset observedAtUtc) =>
        Restore(
            Guid.NewGuid(),
            payoutId,
            merchantId,
            status,
            providerReference,
            failureCode,
            amountMinor,
            currency,
            observedAtUtc);

    public static MerchantPayoutProviderResultRecord Restore(
        Guid resultId,
        Guid payoutId,
        string merchantId,
        MerchantPayoutProviderResultStatus status,
        string? providerReference,
        string? failureCode,
        long amountMinor,
        string currency,
        DateTimeOffset observedAtUtc)
    {
        if (resultId == Guid.Empty) throw new ArgumentException("Result id is required.", nameof(resultId));
        if (payoutId == Guid.Empty) throw new ArgumentException("Payout id is required.", nameof(payoutId));
        if (string.IsNullOrWhiteSpace(merchantId)) throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (amountMinor <= 0) throw new ArgumentOutOfRangeException(nameof(amountMinor));
        var normalizedCurrency = NormalizeCurrency(currency);
        EnsureUtc(observedAtUtc);

        var reference = string.IsNullOrWhiteSpace(providerReference) ? null : providerReference.Trim();
        var failure = string.IsNullOrWhiteSpace(failureCode) ? null : failureCode.Trim();

        if (status == MerchantPayoutProviderResultStatus.Succeeded && reference is null)
            throw new ArgumentException("Successful provider result requires a provider reference.", nameof(providerReference));
        if (status == MerchantPayoutProviderResultStatus.Failed && failure is null)
            throw new ArgumentException("Failed provider result requires a failure code.", nameof(failureCode));

        return new MerchantPayoutProviderResultRecord(
            resultId,
            payoutId,
            merchantId.Trim(),
            status,
            reference,
            failure,
            amountMinor,
            normalizedCurrency,
            observedAtUtc);
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

public sealed record MerchantPayoutReconciliationRecord
{
    public Guid ReconciliationId { get; }
    public Guid PayoutId { get; }
    public Guid ResultId { get; }
    public string MerchantId { get; }
    public MerchantPayoutReconciliationStatus Status { get; }
    public string ReasonCode { get; }
    public DateTimeOffset EvaluatedAtUtc { get; }

    private MerchantPayoutReconciliationRecord(
        Guid reconciliationId,
        Guid payoutId,
        Guid resultId,
        string merchantId,
        MerchantPayoutReconciliationStatus status,
        string reasonCode,
        DateTimeOffset evaluatedAtUtc)
    {
        ReconciliationId = reconciliationId;
        PayoutId = payoutId;
        ResultId = resultId;
        MerchantId = merchantId;
        Status = status;
        ReasonCode = reasonCode;
        EvaluatedAtUtc = evaluatedAtUtc;
    }

    public static MerchantPayoutReconciliationRecord Create(
        Guid payoutId,
        Guid resultId,
        string merchantId,
        MerchantPayoutReconciliationStatus status,
        string reasonCode,
        DateTimeOffset evaluatedAtUtc) =>
        Restore(Guid.NewGuid(), payoutId, resultId, merchantId, status, reasonCode, evaluatedAtUtc);

    public static MerchantPayoutReconciliationRecord Restore(
        Guid reconciliationId,
        Guid payoutId,
        Guid resultId,
        string merchantId,
        MerchantPayoutReconciliationStatus status,
        string reasonCode,
        DateTimeOffset evaluatedAtUtc)
    {
        if (reconciliationId == Guid.Empty) throw new ArgumentException("Reconciliation id is required.", nameof(reconciliationId));
        if (payoutId == Guid.Empty) throw new ArgumentException("Payout id is required.", nameof(payoutId));
        if (resultId == Guid.Empty) throw new ArgumentException("Result id is required.", nameof(resultId));
        if (string.IsNullOrWhiteSpace(merchantId)) throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (string.IsNullOrWhiteSpace(reasonCode)) throw new ArgumentException("Reason code is required.", nameof(reasonCode));
        if (evaluatedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", nameof(evaluatedAtUtc));

        return new MerchantPayoutReconciliationRecord(
            reconciliationId,
            payoutId,
            resultId,
            merchantId.Trim(),
            status,
            reasonCode.Trim(),
            evaluatedAtUtc);
    }
}
