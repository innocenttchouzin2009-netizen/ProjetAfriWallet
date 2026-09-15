namespace AfriWallet.Notifications.Domain;

public sealed class InAppNotification
{
    private InAppNotification(
        Guid id,
        Guid userId,
        Guid eventId,
        Guid paymentRequestId,
        PaymentRequestEventKind eventKind,
        DateTimeOffset createdAtUtc,
        Guid? transferId,
        DateTimeOffset? readAtUtc)
    {
        Id = id;
        UserId = userId;
        EventId = eventId;
        PaymentRequestId = paymentRequestId;
        EventKind = eventKind;
        CreatedAtUtc = createdAtUtc;
        TransferId = transferId;
        ReadAtUtc = readAtUtc;
    }

    public Guid Id { get; }
    public Guid UserId { get; }
    public Guid EventId { get; }
    public Guid PaymentRequestId { get; }
    public PaymentRequestEventKind EventKind { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid? TransferId { get; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public bool IsRead => ReadAtUtc is not null;

    public static InAppNotification New(Guid userId, PaymentRequestEvent paymentRequestEvent)
    {
        ArgumentNullException.ThrowIfNull(paymentRequestEvent);
        return Restore(
            Guid.NewGuid(),
            userId,
            paymentRequestEvent.EventId,
            paymentRequestEvent.PaymentRequestId,
            paymentRequestEvent.Kind,
            paymentRequestEvent.OccurredAtUtc,
            paymentRequestEvent.TransferId,
            null);
    }

    public static InAppNotification Restore(
        Guid id,
        Guid userId,
        Guid eventId,
        Guid paymentRequestId,
        PaymentRequestEventKind eventKind,
        DateTimeOffset createdAtUtc,
        Guid? transferId,
        DateTimeOffset? readAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Notification id cannot be empty.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        if (eventId == Guid.Empty) throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        if (paymentRequestId == Guid.Empty) throw new ArgumentException("Payment request id cannot be empty.", nameof(paymentRequestId));
        if (!Enum.IsDefined(eventKind)) throw new ArgumentOutOfRangeException(nameof(eventKind));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        if (readAtUtc is not null)
        {
            EnsureUtc(readAtUtc.Value, nameof(readAtUtc));
            if (readAtUtc.Value < createdAtUtc) throw new ArgumentException("Read time cannot precede creation.", nameof(readAtUtc));
        }
        if (eventKind == PaymentRequestEventKind.Paid && (transferId is null || transferId == Guid.Empty))
            throw new ArgumentException("Paid notifications require a transfer id.", nameof(transferId));
        if (eventKind != PaymentRequestEventKind.Paid && transferId is not null)
            throw new ArgumentException("Only paid notifications may carry a transfer id.", nameof(transferId));

        return new InAppNotification(id, userId, eventId, paymentRequestId, eventKind, createdAtUtc, transferId, readAtUtc);
    }

    public bool MarkRead(DateTimeOffset readAtUtc)
    {
        EnsureUtc(readAtUtc, nameof(readAtUtc));
        if (readAtUtc < CreatedAtUtc) throw new ArgumentException("Read time cannot precede creation.", nameof(readAtUtc));
        if (ReadAtUtc is not null) return false;
        ReadAtUtc = readAtUtc;
        return true;
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
