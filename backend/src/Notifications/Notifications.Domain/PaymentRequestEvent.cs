namespace AfriWallet.Notifications.Domain;

public enum PaymentRequestEventKind
{
    Created = 1,
    Accepted = 2,
    Declined = 3,
    Cancelled = 4,
    Expired = 5,
    Paid = 6
}

public sealed record PaymentRequestEvent
{
    public Guid EventId { get; }
    public Guid PaymentRequestId { get; }
    public PaymentRequestEventKind Kind { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public Guid? TransferId { get; }

    private PaymentRequestEvent(
        Guid eventId,
        Guid paymentRequestId,
        PaymentRequestEventKind kind,
        DateTimeOffset occurredAtUtc,
        Guid? transferId)
    {
        EventId = eventId;
        PaymentRequestId = paymentRequestId;
        Kind = kind;
        OccurredAtUtc = occurredAtUtc;
        TransferId = transferId;
    }

    public static PaymentRequestEvent Create(
        Guid eventId,
        Guid paymentRequestId,
        PaymentRequestEventKind kind,
        DateTimeOffset occurredAtUtc,
        Guid? transferId = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        }

        if (paymentRequestId == Guid.Empty)
        {
            throw new ArgumentException("Payment request id cannot be empty.", nameof(paymentRequestId));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported payment request event kind.");
        }

        if (occurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Event timestamp must be UTC.", nameof(occurredAtUtc));
        }

        if (kind == PaymentRequestEventKind.Paid)
        {
            if (transferId is null || transferId == Guid.Empty)
            {
                throw new ArgumentException("Paid events require a transfer id.", nameof(transferId));
            }
        }
        else if (transferId is not null)
        {
            throw new ArgumentException("Only Paid events may carry a transfer id.", nameof(transferId));
        }

        return new PaymentRequestEvent(eventId, paymentRequestId, kind, occurredAtUtc, transferId);
    }

    public static PaymentRequestEvent New(
        Guid paymentRequestId,
        PaymentRequestEventKind kind,
        DateTimeOffset occurredAtUtc,
        Guid? transferId = null) =>
        Create(Guid.NewGuid(), paymentRequestId, kind, occurredAtUtc, transferId);
}
