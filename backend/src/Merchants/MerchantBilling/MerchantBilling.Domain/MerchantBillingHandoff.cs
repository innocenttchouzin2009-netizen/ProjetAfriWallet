namespace AfriWallet.Merchants.Billing.Domain;

public enum MerchantBillingHandoffStatus
{
    Pending = 0,
    Completed = 1
}

public sealed class MerchantBillingHandoff
{
    private MerchantBillingHandoff(
        Guid handoffId,
        Guid captureExecutionId,
        string merchantId,
        MerchantBillingHandoffStatus status,
        Guid? receivableId,
        int attemptCount,
        string? lastError,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        HandoffId = handoffId;
        CaptureExecutionId = captureExecutionId;
        MerchantId = merchantId;
        Status = status;
        ReceivableId = receivableId;
        AttemptCount = attemptCount;
        LastError = lastError;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid HandoffId { get; }
    public Guid CaptureExecutionId { get; }
    public string MerchantId { get; }
    public MerchantBillingHandoffStatus Status { get; private set; }
    public Guid? ReceivableId { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static MerchantBillingHandoff Create(
        Guid captureExecutionId,
        string merchantId,
        DateTimeOffset now)
    {
        if (captureExecutionId == Guid.Empty)
            throw new ArgumentException("Capture execution id is required.", nameof(captureExecutionId));
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        EnsureUtc(now);

        return new(
            Guid.NewGuid(),
            captureExecutionId,
            merchantId.Trim().ToUpperInvariant(),
            MerchantBillingHandoffStatus.Pending,
            null,
            0,
            null,
            now,
            now);
    }

    public static MerchantBillingHandoff Restore(
        Guid handoffId,
        Guid captureExecutionId,
        string merchantId,
        MerchantBillingHandoffStatus status,
        Guid? receivableId,
        int attemptCount,
        string? lastError,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (handoffId == Guid.Empty) throw new ArgumentException("Handoff id is required.", nameof(handoffId));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (attemptCount < 0) throw new ArgumentOutOfRangeException(nameof(attemptCount));
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);

        if (status == MerchantBillingHandoffStatus.Completed && receivableId is null)
            throw new InvalidOperationException("Completed billing handoff requires a receivable id.");

        return new(
            handoffId,
            captureExecutionId,
            merchantId.Trim().ToUpperInvariant(),
            status,
            receivableId,
            attemptCount,
            lastError,
            createdAtUtc,
            updatedAtUtc);
    }

    public void BeginAttempt(DateTimeOffset now)
    {
        EnsureUtc(now);
        if (Status == MerchantBillingHandoffStatus.Completed) return;
        AttemptCount = checked(AttemptCount + 1);
        LastError = null;
        UpdatedAtUtc = now;
    }

    public void Complete(Guid receivableId, DateTimeOffset now)
    {
        if (receivableId == Guid.Empty) throw new ArgumentException("Receivable id is required.", nameof(receivableId));
        EnsureUtc(now);
        if (Status == MerchantBillingHandoffStatus.Completed)
        {
            if (ReceivableId == receivableId) return;
            throw new InvalidOperationException("Billing handoff already completed with a different receivable.");
        }

        ReceivableId = receivableId;
        Status = MerchantBillingHandoffStatus.Completed;
        LastError = null;
        UpdatedAtUtc = now;
    }

    public void RecordFailure(string error, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (Status == MerchantBillingHandoffStatus.Completed) return;
        LastError = string.IsNullOrWhiteSpace(error) ? "billing_handoff_failed" : error.Trim();
        UpdatedAtUtc = now;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.");
    }
}
