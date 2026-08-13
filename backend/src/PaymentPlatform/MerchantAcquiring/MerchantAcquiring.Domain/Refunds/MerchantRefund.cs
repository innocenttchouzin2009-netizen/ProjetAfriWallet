namespace MerchantAcquiring.Domain.Refunds;

public sealed class MerchantRefund
{
    public MerchantRefund(
        Guid refundId,
        Guid paymentId,
        long amountMinor,
        string reason)
    {
        if (refundId == Guid.Empty ||
            paymentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Refund and payment IDs are required.");
        }

        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amountMinor));

        RefundId = refundId;
        PaymentId = paymentId;
        AmountMinor = amountMinor;
        Reason = reason.Trim();
    }

    public Guid RefundId { get; }

    public Guid PaymentId { get; }

    public long AmountMinor { get; }

    public string Reason { get; }

    public RefundStatus Status { get; private set; }
        = RefundStatus.Created;

    public DateTime CreatedAtUtc { get; } =
        DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; private set; }

    public void Complete()
    {
        if (Status != RefundStatus.Created)
            throw new InvalidOperationException(
                "Refund cannot complete.");

        Status = RefundStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Fail()
    {
        if (Status == RefundStatus.Completed)
            throw new InvalidOperationException(
                "Completed refund is immutable.");

        Status = RefundStatus.Failed;
    }
}

public enum RefundStatus
{
    Created,
    Completed,
    Failed
}
