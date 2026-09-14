using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed class AuthorizedPaymentRequestQueryService(
    IPaymentRequestQueryRepository queryRepository,
    IPaymentRequestOwnedWalletReader ownedWalletReader,
    IPaymentRequestOwnedRecipientReferenceReader ownedRecipientReferenceReader)
{
    public async Task<PaymentRequestQueryPage> ListInboxAsync(
        AuthorizedInboxQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateCommon(query.UserId, query.Statuses, query.Page, query.Order);
        cancellationToken.ThrowIfCancellationRequested();

        var wallets = await ownedWalletReader.ListOwnedWalletIdsAsync(query.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Owned wallet reader returned null.");
        var references = await ownedRecipientReferenceReader.ListOwnedRecipientReferencesAsync(query.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Owned recipient reference reader returned null.");

        var authorizedWallets = wallets
            .Where(wallet => wallet.Value != Guid.Empty)
            .Distinct()
            .ToArray();
        var authorizedReferences = references
            .Where(reference => reference is not null)
            .Distinct()
            .ToArray();

        if (authorizedWallets.Length == 0 && authorizedReferences.Length == 0)
        {
            return Empty(query.Page);
        }

        return await queryRepository.ListReceivedAsync(
            new ReceivedPaymentRequestsQuery(
                authorizedReferences,
                authorizedWallets,
                query.Statuses,
                query.Page,
                query.Order),
            cancellationToken);
    }

    public async Task<PaymentRequestQueryPage> ListOutboxAsync(
        AuthorizedOutboxQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateCommon(query.UserId, query.Statuses, query.Page, query.Order);
        cancellationToken.ThrowIfCancellationRequested();

        var wallets = await ownedWalletReader.ListOwnedWalletIdsAsync(query.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Owned wallet reader returned null.");
        var authorizedWallets = wallets
            .Where(wallet => wallet.Value != Guid.Empty)
            .Distinct()
            .ToArray();

        if (authorizedWallets.Length == 0)
        {
            return Empty(query.Page);
        }

        return await queryRepository.ListSentAsync(
            new SentPaymentRequestsQuery(
                authorizedWallets,
                query.Statuses,
                query.Page,
                query.Order),
            cancellationToken);
    }

    private static void ValidateCommon(
        Guid userId,
        IReadOnlyCollection<PaymentRequestStatus>? statuses,
        PaymentRequestPageRequest page,
        PaymentRequestTemporalOrder order)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user id cannot be empty.", nameof(userId));
        }

        ArgumentNullException.ThrowIfNull(page);
        _ = PaymentRequestPageRequest.Create(page.PageNumber, page.PageSize);

        if (order is not (PaymentRequestTemporalOrder.NewestFirst or PaymentRequestTemporalOrder.OldestFirst))
        {
            throw new ArgumentOutOfRangeException(nameof(order));
        }

        if (statuses is not null && statuses.Any(status => !Enum.IsDefined(status)))
        {
            throw new ArgumentOutOfRangeException(nameof(statuses), "Payment request status filter contains an unsupported value.");
        }
    }

    private static PaymentRequestQueryPage Empty(PaymentRequestPageRequest page) =>
        new([], page.PageNumber, page.PageSize, 0, false);
}
