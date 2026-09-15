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
        DateTimeOffset? readAtUtc,
        DateTimeOffset? archivedAtUtc)
    {
        Id = id;
        UserId = userId;
        EventId = eventId;
        PaymentRequestId = paymentRequestId;
        EventKind = eventKind;
        CreatedAtUtc = createdAtUtc;
        TransferId = transferId;
        ReadAtUtc = readAtUtc;
        ArchivedAtUtc = archivedAtUtc;
    }

    public Guid Id { get; }
    public Guid UserId { get; }
    public Guid EventId { get; }
    public Guid PaymentRequestId { get; }
    public PaymentRequestEventKind EventKind { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid? TransferId { get; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public bool IsRead => ReadAtUtc is not null;
    public bool IsArchived => ArchivedAtUtc is not null;

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
            null,
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
        DateTimeOffset? readAtUtc,
        DateTimeOffset? archivedAtUtc = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Notification id cannot be empty.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        if (eventId == Guid.Empty) throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        if (paymentRequestId == Guid.Empty) throw new ArgumentException("Payment request id cannot be empty.", nameof(paymentRequestId));
        if (!Enum.IsDefined(eventKind)) throw new ArgumentOutOfRangeException(nameof(eventKind));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        if (eventKind == PaymentRequestEventKind.Paid && transferId is null)
            throw new ArgumentException("Paid notifications require a transfer id.", nameof(transferId));
        if (eventKind != PaymentRequestEventKind.Paid && transferId is not null)
            throw new ArgumentException("Only paid notifications may contain a transfer id.", nameof(transferId));

        if (readAtUtc is not null)
        {
            EnsureUtc(readAtUtc.Value, nameof(readAtUtc));
            if (readAtUtc.Value < createdAtUtc)
                throw new ArgumentException("Read timestamp cannot precede creation.", nameof(readAtUtc));
        }

        if (archivedAtUtc is not null)
        {
            EnsureUtc(archivedAtUtc.Value, nameof(archivedAtUtc));
            if (archivedAtUtc.Value < createdAtUtc)
                throw new ArgumentException("Archive timestamp cannot precede creation.", nameof(archivedAtUtc));
        }

        return new InAppNotification(
            id,
            userId,
            eventId,
            paymentRequestId,
            eventKind,
            createdAtUtc,
            transferId,
            readAtUtc,
            archivedAtUtc);
    }

    public bool MarkRead(DateTimeOffset readAtUtc)
    {
        EnsureUtc(readAtUtc, nameof(readAtUtc));
        if (readAtUtc < CreatedAtUtc)
            throw new ArgumentException("Read timestamp cannot precede creation.", nameof(readAtUtc));
        if (IsArchived)
            throw new InvalidOperationException("Archived notification cannot be modified.");
        if (ReadAtUtc is not null) return false;
        ReadAtUtc = readAtUtc;
        return true;
    }

    public bool Archive(DateTimeOffset archivedAtUtc)
    {
        EnsureUtc(archivedAtUtc, nameof(archivedAtUtc));
        if (archivedAtUtc < CreatedAtUtc)
            throw new ArgumentException("Archive timestamp cannot precede creation.", nameof(archivedAtUtc));
        if (ArchivedAtUtc is not null) return false;
        ArchivedAtUtc = archivedAtUtc;
        return true;
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
