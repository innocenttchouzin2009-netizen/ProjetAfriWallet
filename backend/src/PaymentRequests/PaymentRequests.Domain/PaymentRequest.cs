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
        ValidateCore(requesterWalletId, payerReference, currency, amountMinor, correlationId, createdAtUtc, expiresAtUtc);

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

    public static PaymentRequest Restore(
        PaymentRequestId id,
        WalletId requesterWalletId,
        RecipientReference payerReference,
        Currency currency,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset updatedAtUtc,
        PaymentRequestStatus status,
        WalletId? acceptedPayerWalletId,
        DateTimeOffset? acceptedAtUtc,
        Guid? transferId,
        DateTimeOffset? closedAtUtc)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Payment request id cannot be empty.", nameof(id));
        }

        ValidateCore(requesterWalletId, payerReference, currency, amountMinor, correlationId, createdAtUtc, expiresAtUtc);
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < createdAtUtc)
        {
            throw new ArgumentException("Updated timestamp cannot precede creation time.", nameof(updatedAtUtc));
        }

        if (acceptedAtUtc is not null)
        {
            EnsureUtc(acceptedAtUtc.Value, nameof(acceptedAtUtc));
            if (acceptedAtUtc.Value < createdAtUtc || acceptedAtUtc.Value > updatedAtUtc)
            {
                throw new ArgumentException("Acceptance timestamp is inconsistent with request timeline.", nameof(acceptedAtUtc));
            }
        }

        if (closedAtUtc is not null)
        {
            EnsureUtc(closedAtUtc.Value, nameof(closedAtUtc));
            if (closedAtUtc.Value < createdAtUtc || closedAtUtc.Value != updatedAtUtc)
            {
                throw new ArgumentException("Closed timestamp is inconsistent with request timeline.", nameof(closedAtUtc));
            }
        }

        var hasAcceptedWallet = acceptedPayerWalletId is not null;
        var hasAcceptedAt = acceptedAtUtc is not null;
        if (hasAcceptedWallet != hasAcceptedAt)
        {
            throw new ArgumentException("Accepted payer wallet and acceptance timestamp must be present together.");
        }

        if (acceptedPayerWalletId is not null)
        {
            if (acceptedPayerWalletId.Value.Value == Guid.Empty)
            {
                throw new ArgumentException("Accepted payer wallet id cannot be empty.", nameof(acceptedPayerWalletId));
            }

            if (acceptedPayerWalletId.Value == requesterWalletId)
            {
                throw new InvalidOperationException("Requester and payer wallets must be different.");
            }
        }

        switch (status)
        {
            case PaymentRequestStatus.Pending:
                if (acceptedPayerWalletId is not null || transferId is not null || closedAtUtc is not null || updatedAtUtc != createdAtUtc)
                {
                    throw new InvalidOperationException("Pending payment request persistence state is inconsistent.");
                }
                break;

            case PaymentRequestStatus.Accepted:
                if (acceptedPayerWalletId is null || acceptedAtUtc is null || transferId is not null || closedAtUtc is not null || updatedAtUtc != acceptedAtUtc.Value)
                {
                    throw new InvalidOperationException("Accepted payment request persistence state is inconsistent.");
                }
                EnsureNotPastExpiration(expiresAtUtc, acceptedAtUtc.Value);
                break;

            case PaymentRequestStatus.Declined:
                if (acceptedPayerWalletId is not null || transferId is not null || closedAtUtc is null)
                {
                    throw new InvalidOperationException("Declined payment request persistence state is inconsistent.");
                }
                EnsureNotPastExpiration(expiresAtUtc, closedAtUtc.Value);
                break;

            case PaymentRequestStatus.Cancelled:
                if (transferId is not null || closedAtUtc is null)
                {
                    throw new InvalidOperationException("Cancelled payment request persistence state is inconsistent.");
                }
                if (acceptedAtUtc is not null && closedAtUtc.Value < acceptedAtUtc.Value)
                {
                    throw new InvalidOperationException("Cancellation cannot precede acceptance.");
                }
                EnsureNotPastExpiration(expiresAtUtc, closedAtUtc.Value);
                break;

            case PaymentRequestStatus.Expired:
                if (transferId is not null || closedAtUtc is null || expiresAtUtc is null || closedAtUtc.Value < expiresAtUtc.Value)
                {
                    throw new InvalidOperationException("Expired payment request persistence state is inconsistent.");
                }
                if (acceptedAtUtc is not null && closedAtUtc.Value < acceptedAtUtc.Value)
                {
                    throw new InvalidOperationException("Expiration cannot precede acceptance.");
                }
                break;

            case PaymentRequestStatus.Paid:
                if (acceptedPayerWalletId is null || acceptedAtUtc is null || transferId is null || transferId == Guid.Empty || closedAtUtc is null || closedAtUtc.Value < acceptedAtUtc.Value)
                {
                    throw new InvalidOperationException("Paid payment request persistence state is inconsistent.");
                }
                EnsureNotPastExpiration(expiresAtUtc, closedAtUtc.Value);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported payment request status.");
        }

        var restored = new PaymentRequest(
            id,
            requesterWalletId,
            payerReference,
            currency,
            amountMinor,
            correlationId,
            createdAtUtc,
            expiresAtUtc)
        {
            UpdatedAtUtc = updatedAtUtc,
            Status = status,
            AcceptedPayerWalletId = acceptedPayerWalletId,
            AcceptedAtUtc = acceptedAtUtc,
            TransferId = transferId,
            ClosedAtUtc = closedAtUtc
        };

        return restored;
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

    private static void ValidateCore(
        WalletId requesterWalletId,
        RecipientReference payerReference,
        Currency currency,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc)
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

    private void EnsureNotExpired(DateTimeOffset atUtc) => EnsureNotPastExpiration(ExpiresAtUtc, atUtc);

    private static void EnsureNotPastExpiration(DateTimeOffset? expiresAtUtc, DateTimeOffset atUtc)
    {
        if (expiresAtUtc is not null && atUtc >= expiresAtUtc.Value)
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
