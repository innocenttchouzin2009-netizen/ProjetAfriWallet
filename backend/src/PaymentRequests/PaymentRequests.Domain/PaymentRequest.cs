using AfriWallet.P2P.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Domain;

public sealed class PaymentRequest
{
    private PaymentRequest(
        PaymentRequestId id,
        WalletId requesterWalletId,
        RecipientReference payerReference,
        Currency currency,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc)
    {
        Id = id;
        RequesterWalletId = requesterWalletId;
        PayerReference = payerReference;
        Currency = currency;
        AmountMinor = amountMinor;
        CorrelationId = correlationId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        UpdatedAtUtc = createdAtUtc;
        Status = PaymentRequestStatus.Pending;
    }

    public PaymentRequestId Id { get; }
    public WalletId RequesterWalletId { get; }
    public RecipientReference PayerReference { get; }
    public Currency Currency { get; }
    public long AmountMinor { get; }
    public Guid CorrelationId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? ExpiresAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public PaymentRequestStatus Status { get; private set; }
    public WalletId? AcceptedPayerWalletId { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public Guid? TransferId { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public static PaymentRequest Create(
        WalletId requesterWalletId,
        RecipientReference payerReference,
        Currency currency,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(payerReference);
        ArgumentNullException.ThrowIfNull(currency);

        if (requesterWalletId.Value == Guid.Empty)
        {
            throw new ArgumentException("Requester wallet id cannot be empty.", nameof(requesterWalletId));
        }

        if (amountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Payment request amount must be positive.");
        }

        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        if (expiresAtUtc is not null)
        {
            EnsureUtc(expiresAtUtc.Value, nameof(expiresAtUtc));
            if (expiresAtUtc.Value <= createdAtUtc)
            {
                throw new ArgumentException("Expiration must be later than creation time.", nameof(expiresAtUtc));
            }
        }

        return new PaymentRequest(
            PaymentRequestId.New(),
            requesterWalletId,
            payerReference,
            currency,
            amountMinor,
            correlationId,
            createdAtUtc,
            expiresAtUtc);
    }

    public void Accept(WalletId payerWalletId, DateTimeOffset acceptedAtUtc)
    {
        EnsureTransitionTime(acceptedAtUtc, nameof(acceptedAtUtc));
        EnsurePending();
        EnsureNotExpired(acceptedAtUtc);

        if (payerWalletId.Value == Guid.Empty)
        {
            throw new ArgumentException("Payer wallet id cannot be empty.", nameof(payerWalletId));
        }

        if (payerWalletId == RequesterWalletId)
        {
            throw new InvalidOperationException("Requester and payer wallets must be different.");
        }

        Status = PaymentRequestStatus.Accepted;
        AcceptedPayerWalletId = payerWalletId;
        AcceptedAtUtc = acceptedAtUtc;
        UpdatedAtUtc = acceptedAtUtc;
    }

    public void Decline(DateTimeOffset declinedAtUtc)
    {
        EnsureTransitionTime(declinedAtUtc, nameof(declinedAtUtc));
        EnsurePending();
        EnsureNotExpired(declinedAtUtc);
        Close(PaymentRequestStatus.Declined, declinedAtUtc);
    }

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        EnsureTransitionTime(cancelledAtUtc, nameof(cancelledAtUtc));
        EnsurePendingOrAccepted();
        EnsureNotExpired(cancelledAtUtc);
        Close(PaymentRequestStatus.Cancelled, cancelledAtUtc);
    }

    public void Expire(DateTimeOffset expiredAtUtc)
    {
        EnsureTransitionTime(expiredAtUtc, nameof(expiredAtUtc));
        EnsurePendingOrAccepted();

        if (ExpiresAtUtc is null)
        {
            throw new InvalidOperationException("Payment request has no expiration time.");
        }

        if (expiredAtUtc < ExpiresAtUtc.Value)
        {
            throw new InvalidOperationException("Payment request cannot expire before its expiration time.");
        }

        Close(PaymentRequestStatus.Expired, expiredAtUtc);
    }

    public void MarkPaid(Guid transferId, DateTimeOffset paidAtUtc)
    {
        EnsureTransitionTime(paidAtUtc, nameof(paidAtUtc));

        if (Status != PaymentRequestStatus.Accepted)
        {
            throw new InvalidOperationException("Only an accepted payment request can be marked paid.");
        }

        EnsureNotExpired(paidAtUtc);

        if (transferId == Guid.Empty)
        {
            throw new ArgumentException("Transfer id cannot be empty.", nameof(transferId));
        }

        TransferId = transferId;
        Close(PaymentRequestStatus.Paid, paidAtUtc);
    }

    private void EnsurePending()
    {
        if (Status != PaymentRequestStatus.Pending)
        {
            throw new InvalidOperationException("Payment request is not pending.");
        }
    }

    private void EnsurePendingOrAccepted()
    {
        if (Status is not (PaymentRequestStatus.Pending or PaymentRequestStatus.Accepted))
        {
            throw new InvalidOperationException("Payment request is already terminal.");
        }
    }

    private void EnsureNotExpired(DateTimeOffset atUtc)
    {
        if (ExpiresAtUtc is not null && atUtc >= ExpiresAtUtc.Value)
        {
            throw new InvalidOperationException("Payment request has expired.");
        }
    }

    private void EnsureTransitionTime(DateTimeOffset value, string parameterName)
    {
        EnsureUtc(value, parameterName);
        if (value < UpdatedAtUtc)
        {
            throw new ArgumentException("Transition timestamp cannot move backwards.", parameterName);
        }
    }

    private void Close(PaymentRequestStatus status, DateTimeOffset atUtc)
    {
        Status = status;
        ClosedAtUtc = atUtc;
        UpdatedAtUtc = atUtc;
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
