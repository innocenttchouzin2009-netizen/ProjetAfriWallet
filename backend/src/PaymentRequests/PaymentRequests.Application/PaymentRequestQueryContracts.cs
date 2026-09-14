using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Application;

public enum PaymentRequestTemporalOrder
{
    NewestFirst = 1,
    OldestFirst = 2
}

public sealed record PaymentRequestPageRequest
{
    private PaymentRequestPageRequest(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public int PageNumber { get; }
    public int PageSize { get; }

    public static PaymentRequestPageRequest Create(int pageNumber = 0, int pageSize = 20)
    {
        if (pageNumber < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number cannot be negative.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");
        }

        return new PaymentRequestPageRequest(pageNumber, pageSize);
    }
}

public sealed record PaymentRequestQueryItem(
    PaymentRequestId Id,
    WalletId RequesterWalletId,
    RecipientReferenceKind PayerReferenceKind,
    Currency Currency,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset UpdatedAtUtc,
    PaymentRequestStatus Status,
    WalletId? AcceptedPayerWalletId,
    DateTimeOffset? AcceptedAtUtc,
    Guid? TransferId,
    DateTimeOffset? ClosedAtUtc);

public sealed record PaymentRequestQueryPage(
    IReadOnlyList<PaymentRequestQueryItem> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    bool HasMore);

public sealed record ReceivedPaymentRequestsQuery(
    IReadOnlyCollection<RecipientReference> RecipientReferences,
    IReadOnlyCollection<WalletId> RecipientWalletIds,
    IReadOnlyCollection<PaymentRequestStatus>? Statuses,
    PaymentRequestPageRequest Page,
    PaymentRequestTemporalOrder Order = PaymentRequestTemporalOrder.NewestFirst);

public sealed record SentPaymentRequestsQuery(
    IReadOnlyCollection<WalletId> RequesterWalletIds,
    IReadOnlyCollection<PaymentRequestStatus>? Statuses,
    PaymentRequestPageRequest Page,
    PaymentRequestTemporalOrder Order = PaymentRequestTemporalOrder.NewestFirst);
