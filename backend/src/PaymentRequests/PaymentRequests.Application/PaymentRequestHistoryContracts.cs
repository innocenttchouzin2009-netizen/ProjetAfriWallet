using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Application;

public enum PaymentRequestHistoryDirection
{
    All = 1,
    Sent = 2,
    Received = 3
}

public sealed record PaymentRequestHistoryQuery(
    Guid UserId,
    PaymentRequestHistoryDirection Direction,
    IReadOnlyCollection<PaymentRequestStatus>? Statuses,
    PaymentRequestPageRequest Page,
    PaymentRequestTemporalOrder Order = PaymentRequestTemporalOrder.NewestFirst);

public sealed record PaymentRequestHistoryReadQuery(
    IReadOnlyCollection<WalletId> OwnedWalletIds,
    IReadOnlyCollection<RecipientReference> OwnedRecipientReferences,
    PaymentRequestHistoryDirection Direction,
    IReadOnlyCollection<PaymentRequestStatus>? Statuses,
    PaymentRequestPageRequest Page,
    PaymentRequestTemporalOrder Order = PaymentRequestTemporalOrder.NewestFirst);

public sealed record PaymentRequestHistoryItem(
    PaymentRequestQueryItem Request,
    PaymentRequestHistoryDirection Direction);

public sealed record PaymentRequestHistoryPage(
    IReadOnlyList<PaymentRequestHistoryItem> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    bool HasMore);

public interface IPaymentRequestHistoryReader
{
    Task<PaymentRequestHistoryPage> ListAsync(
        PaymentRequestHistoryReadQuery query,
        CancellationToken cancellationToken = default);
}
