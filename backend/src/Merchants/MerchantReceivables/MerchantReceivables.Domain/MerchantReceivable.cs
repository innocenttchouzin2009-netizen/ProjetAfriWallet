namespace AfriWallet.Merchants.Receivables.Domain;

public enum MerchantReceivableStatus
{
    Open = 0,
    Settled = 1
}

public sealed record MerchantFeeSchedule
{
    public MerchantFeeSchedule(string merchantId, string currency, int percentageBasisPoints, long fixedFeeMinor)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        MerchantId = merchantId.Trim().ToUpperInvariant();
        Currency = NormalizeCurrency(currency);
        if (percentageBasisPoints is < 0 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(percentageBasisPoints));
        if (fixedFeeMinor < 0)
            throw new ArgumentOutOfRangeException(nameof(fixedFeeMinor));
        PercentageBasisPoints = percentageBasisPoints;
        FixedFeeMinor = fixedFeeMinor;
    }

    public string MerchantId { get; }
    public string Currency { get; }
    public int PercentageBasisPoints { get; }
    public long FixedFeeMinor { get; }

    public long CalculateFee(long grossAmountMinor)
    {
        if (grossAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(grossAmountMinor));

        var variable = decimal.Round(
            grossAmountMinor * (PercentageBasisPoints / 10_000m),
            0,
            MidpointRounding.AwayFromZero);

        return checked(decimal.ToInt64(variable) + FixedFeeMinor);
    }

    internal static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency is required.", nameof(value));
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must contain three ISO-like letters.", nameof(value));
        return normalized;
    }
}

public sealed class MerchantReceivable
{
    private MerchantReceivable(
        Guid receivableId,
        Guid captureExecutionId,
        Guid decisionId,
        Guid paymentIntentId,
        string merchantId,
        string currency,
        long grossAmountMinor,
        long feeAmountMinor,
        long netAmountMinor,
        string idempotencyKey,
        MerchantReceivableStatus status,
        Guid? settlementId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? settledAtUtc)
    {
        ReceivableId = receivableId;
        CaptureExecutionId = captureExecutionId;
        DecisionId = decisionId;
        PaymentIntentId = paymentIntentId;
        MerchantId = merchantId;
        Currency = currency;
        GrossAmountMinor = grossAmountMinor;
        FeeAmountMinor = feeAmountMinor;
        NetAmountMinor = netAmountMinor;
        IdempotencyKey = idempotencyKey;
        Status = status;
        SettlementId = settlementId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        SettledAtUtc = settledAtUtc;
    }

    public Guid ReceivableId { get; }
    public Guid CaptureExecutionId { get; }
    public Guid DecisionId { get; }
    public Guid PaymentIntentId { get; }
    public string MerchantId { get; }
    public string Currency { get; }
    public long GrossAmountMinor { get; }
    public long FeeAmountMinor { get; }
    public long NetAmountMinor { get; }
    public string IdempotencyKey { get; }
    public MerchantReceivableStatus Status { get; private set; }
    public Guid? SettlementId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? SettledAtUtc { get; private set; }

    public static MerchantReceivable Create(
        Guid captureExecutionId,
        Guid decisionId,
        Guid paymentIntentId,
        string merchantId,
        long grossAmountMinor,
        string currency,
        MerchantFeeSchedule feeSchedule,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (captureExecutionId == Guid.Empty || decisionId == Guid.Empty || paymentIntentId == Guid.Empty)
            throw new ArgumentException("Capture, decision and payment intent ids are required.");
        if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Merchant id and idempotency key are required.");
        if (grossAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(grossAmountMinor));
        EnsureUtc(now);

        var normalizedMerchant = merchantId.Trim().ToUpperInvariant();
        var normalizedCurrency = MerchantFeeSchedule.NormalizeCurrency(currency);
        if (!string.Equals(feeSchedule.MerchantId, normalizedMerchant, StringComparison.Ordinal) ||
            !string.Equals(feeSchedule.Currency, normalizedCurrency, StringComparison.Ordinal))
            throw new InvalidOperationException("Fee schedule does not match the merchant/currency receivable.");

        var fee = feeSchedule.CalculateFee(grossAmountMinor);
        if (fee < 0 || fee >= grossAmountMinor)
            throw new InvalidOperationException("Calculated merchant fee must be lower than the gross amount.");

        return new MerchantReceivable(
            Guid.NewGuid(),
            captureExecutionId,
            decisionId,
            paymentIntentId,
            normalizedMerchant,
            normalizedCurrency,
            grossAmountMinor,
            fee,
            checked(grossAmountMinor - fee),
            idempotencyKey.Trim(),
            MerchantReceivableStatus.Open,
            null,
            now,
            now,
            null);
    }

    public static MerchantReceivable Restore(
        Guid receivableId,
        Guid captureExecutionId,
        Guid decisionId,
        Guid paymentIntentId,
        string merchantId,
        string currency,
        long grossAmountMinor,
        long feeAmountMinor,
        long netAmountMinor,
        string idempotencyKey,
        MerchantReceivableStatus status,
        Guid? settlementId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? settledAtUtc)
    {
        if (receivableId == Guid.Empty) throw new ArgumentException("Receivable id is required.", nameof(receivableId));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (grossAmountMinor <= 0 || feeAmountMinor < 0 || netAmountMinor <= 0 ||
            checked(feeAmountMinor + netAmountMinor) != grossAmountMinor)
            throw new InvalidOperationException("Receivable monetary invariants are invalid.");
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (settledAtUtc is not null) EnsureUtc(settledAtUtc.Value);
        return new(
            receivableId, captureExecutionId, decisionId, paymentIntentId,
            merchantId.Trim().ToUpperInvariant(), MerchantFeeSchedule.NormalizeCurrency(currency),
            grossAmountMinor, feeAmountMinor, netAmountMinor, idempotencyKey.Trim(),
            status, settlementId, createdAtUtc, updatedAtUtc, settledAtUtc);
    }

    public void ApplySettlementReceipt(Guid settlementId, long settledAmountMinor, string currency, DateTimeOffset settledAtUtc)
    {
        if (settlementId == Guid.Empty) throw new ArgumentException("Settlement id is required.", nameof(settlementId));
        EnsureUtc(settledAtUtc);

        if (Status == MerchantReceivableStatus.Settled)
        {
            if (SettlementId == settlementId) return;
            throw new InvalidOperationException("Receivable is already settled by another settlement.");
        }

        if (!string.Equals(Currency, MerchantFeeSchedule.NormalizeCurrency(currency), StringComparison.Ordinal))
            throw new InvalidOperationException("Settlement currency does not match the receivable.");
        if (settledAmountMinor != NetAmountMinor)
            throw new InvalidOperationException("Settlement amount must equal the receivable net amount.");

        Status = MerchantReceivableStatus.Settled;
        SettlementId = settlementId;
        SettledAtUtc = settledAtUtc;
        UpdatedAtUtc = settledAtUtc;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.");
    }
}
