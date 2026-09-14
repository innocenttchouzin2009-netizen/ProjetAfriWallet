using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed record AuthorizedInboxQuery(
    Guid UserId,
    IReadOnlyCollection<PaymentRequestStatus>? Statuses,
    PaymentRequestPageRequest Page,
    PaymentRequestTemporalOrder Order = PaymentRequestTemporalOrder.NewestFirst);

public sealed record AuthorizedOutboxQuery(
    Guid UserId,
    IReadOnlyCollection<PaymentRequestStatus>? Statuses,
    PaymentRequestPageRequest Page,
    PaymentRequestTemporalOrder Order = PaymentRequestTemporalOrder.NewestFirst);

public interface IPaymentRequestOwnedWalletReader
{
    Task<IReadOnlyCollection<WalletId>> ListOwnedWalletIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public interface IPaymentRequestOwnedRecipientReferenceReader
{
    Task<IReadOnlyCollection<RecipientReference>> ListOwnedRecipientReferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
