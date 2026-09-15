using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Domain;

public enum PaymentRequestNotificationKind
{
    Created = 1,
    Accepted = 2,
    Declined = 3,
    Cancelled = 4,
    Expired = 5,
    Paid = 6
}

public enum PaymentRequestNotificationAudience
{
    Requester = 1,
    Payer = 2
}

public enum PaymentRequestNotificationReadStatus
{
    Unread = 1,
    Read = 2
}

public readonly record struct PaymentRequestNotificationId
{
    public Guid Value { get; }

    private PaymentRequestNotificationId(Guid value) => Value = value;

    public static PaymentRequestNotificationId New() => new(Guid.NewGuid());

    public static PaymentRequestNotificationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Payment request notification id cannot be empty.", nameof(value));
        }

        return new PaymentRequestNotificationId(value);
    }

    public override string ToString() => Value.ToString();
}

public sealed class PaymentRequestNotification
{
    private PaymentRequestNotification(
        PaymentRequestNotificationId id,
        Guid sourceEventId,
        PaymentRequestId paymentRequestId,
        PaymentRequestNotificationKind kind,
        PaymentRequestNotificationAudience audience,
        Guid recipientOwnerId,
        WalletId requesterWalletId,
        Currency currency,
        long amountMinor,
        DateTimeOffset occurredAtUtc,
        PaymentRequestStatus requestStatus,
        DateTimeOffset? expiresAtUtc,
        Guid? transferId)
    {
        Id = id;
        SourceEventId = sourceEventId;
        PaymentRequestId = paymentRequestId;
        Kind = kind;
        Audience = audience;
        RecipientOwnerId = recipientOwnerId;
        RequesterWalletId = requesterWalletId;
        Currency = currency;
        AmountMinor = amountMinor;
        OccurredAtUtc = occurredAtUtc;
        RequestStatus = requestStatus;
        ExpiresAtUtc = expiresAtUtc;
        TransferId = transferId;
        ReadStatus = PaymentRequestNotificationReadStatus.Unread;
    }

    public PaymentRequestNotificationId Id { get; }
    public Guid SourceEventId { get; }
    public PaymentRequestId PaymentRequestId { get; }
    public PaymentRequestNotificationKind Kind { get; }
    public PaymentRequestNotificationAudience Audience { get; }
    public Guid RecipientOwnerId { get; }
    public WalletId RequesterWalletId { get; }
    public Currency Currency { get; }
    public long AmountMinor { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public PaymentRequestStatus RequestStatus { get; }
    public DateTimeOffset? ExpiresAtUtc { get; }
    public Guid? TransferId { get; }
    public PaymentRequestNotificationReadStatus ReadStatus { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    public static PaymentRequestNotification Project(
        Guid sourceEventId,
        PaymentRequestId paymentRequestId,
        PaymentRequestNotificationKind kind,
        PaymentRequestNotificationAudience audience,
        Guid recipientOwnerId,
        WalletId requesterWalletId,
        Currency currency,
        long amountMinor,
        DateTimeOffset occurredAtUtc,
        PaymentRequestStatus requestStatus,
        DateTimeOffset? expiresAtUtc = null,
        Guid? transferId = null)
    {
        ArgumentNullException.ThrowIfNull(currency);

        if (sourceEventId == Guid.Empty)
        {
            throw new ArgumentException("Source lifecycle event id cannot be empty.", nameof(sourceEventId));
        }

        if (paymentRequestId.Value == Guid.Empty)
        {
            throw new ArgumentException("Payment request id cannot be empty.", nameof(paymentRequestId));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (!Enum.IsDefined(audience))
        {
            throw new ArgumentOutOfRangeException(nameof(audience));
        }

        if (recipientOwnerId == Guid.Empty)
        {
            throw new ArgumentException("Notification recipient owner id cannot be empty.", nameof(recipientOwnerId));
        }

        if (requesterWalletId.Value == Guid.Empty)
        {
            throw new ArgumentException("Requester wallet id cannot be empty.", nameof(requesterWalletId));
        }

        if (amountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Notification amount must be positive.");
        }

        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        if (expiresAtUtc is not null)
        {
            EnsureUtc(expiresAtUtc.Value, nameof(expiresAtUtc));
        }

        var expectedStatus = kind switch
        {
            PaymentRequestNotificationKind.Created => PaymentRequestStatus.Pending,
            PaymentRequestNotificationKind.Accepted => PaymentRequestStatus.Accepted,
            PaymentRequestNotificationKind.Declined => PaymentRequestStatus.Declined,
            PaymentRequestNotificationKind.Cancelled => PaymentRequestStatus.Cancelled,
            PaymentRequestNotificationKind.Expired => PaymentRequestStatus.Expired,
            PaymentRequestNotificationKind.Paid => PaymentRequestStatus.Paid,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        if (requestStatus != expectedStatus)
        {
            throw new InvalidOperationException(
                $"Notification kind {kind} requires payment request status {expectedStatus}, not {requestStatus}.");
        }

        if (kind == PaymentRequestNotificationKind.Paid)
        {
            if (transferId is null || transferId == Guid.Empty)
            {
                throw new ArgumentException("Paid notification requires a non-empty transfer id.", nameof(transferId));
            }
        }
        else if (transferId is not null)
        {
            throw new ArgumentException("Only a paid notification may contain a transfer id.", nameof(transferId));
        }

        return new PaymentRequestNotification(
            PaymentRequestNotificationId.New(),
            sourceEventId,
            paymentRequestId,
            kind,
            audience,
            recipientOwnerId,
            requesterWalletId,
            currency,
            amountMinor,
            occurredAtUtc,
            requestStatus,
            expiresAtUtc,
            transferId);
    }

    public void MarkRead(DateTimeOffset readAtUtc)
    {
        EnsureUtc(readAtUtc, nameof(readAtUtc));
        if (readAtUtc < OccurredAtUtc)
        {
            throw new ArgumentException("Notification cannot be read before it occurred.", nameof(readAtUtc));
        }

        if (ReadStatus == PaymentRequestNotificationReadStatus.Read)
        {
            if (ReadAtUtc is not null && readAtUtc < ReadAtUtc.Value)
            {
                throw new ArgumentException("Notification read timestamp cannot move backwards.", nameof(readAtUtc));
            }

            return;
        }

        ReadStatus = PaymentRequestNotificationReadStatus.Read;
        ReadAtUtc = readAtUtc;
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
