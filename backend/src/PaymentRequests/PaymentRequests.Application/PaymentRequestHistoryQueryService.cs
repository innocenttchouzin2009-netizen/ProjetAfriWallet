using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed class PaymentRequestHistoryQueryService(
    IPaymentRequestHistoryReader historyReader,
    IPaymentRequestOwnedWalletReader ownedWalletReader,
    IPaymentRequestOwnedRecipientReferenceReader ownedRecipientReferenceReader)
{
    public async Task<PaymentRequestHistoryPage> ListAsync(
        PaymentRequestHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        Validate(query);
        cancellationToken.ThrowIfCancellationRequested();

        var wallets = await ownedWalletReader.ListOwnedWalletIdsAsync(query.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Owned wallet reader returned null.");

        var ownedWallets = wallets
            .Where(wallet => wallet.Value != Guid.Empty)
            .Distinct()
            .ToArray();

        RecipientReference[] ownedReferences = [];
        if (query.Direction is PaymentRequestHistoryDirection.All or PaymentRequestHistoryDirection.Received)
        {
            var references = await ownedRecipientReferenceReader.ListOwnedRecipientReferencesAsync(query.UserId, cancellationToken)
                ?? throw new InvalidOperationException("Owned recipient reference reader returned null.");
            ownedReferences = references
                .Where(reference => reference is not null)
                .Distinct()
                .ToArray();
        }

        var hasScope = query.Direction switch
        {
            PaymentRequestHistoryDirection.Sent => ownedWallets.Length > 0,
            PaymentRequestHistoryDirection.Received => ownedWallets.Length > 0 || ownedReferences.Length > 0,
            PaymentRequestHistoryDirection.All => ownedWallets.Length > 0 || ownedReferences.Length > 0,
            _ => false
        };

        if (!hasScope)
        {
            return Empty(query.Page);
        }

        return await historyReader.ListAsync(
            new PaymentRequestHistoryReadQuery(
                ownedWallets,
                ownedReferences,
                query.Direction,
                query.Statuses,
                query.Page,
                query.Order),
            cancellationToken);
    }

    private static void Validate(PaymentRequestHistoryQuery query)
    {
        if (query.UserId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user id cannot be empty.", nameof(query));
        }

        ArgumentNullException.ThrowIfNull(query.Page);
        _ = PaymentRequestPageRequest.Create(query.Page.PageNumber, query.Page.PageSize);

        if (!Enum.IsDefined(query.Direction))
        {
            throw new ArgumentOutOfRangeException(nameof(query.Direction));
        }

        if (query.Order is not (PaymentRequestTemporalOrder.NewestFirst or PaymentRequestTemporalOrder.OldestFirst))
        {
            throw new ArgumentOutOfRangeException(nameof(query.Order));
        }

        if (query.Statuses is not null && query.Statuses.Any(status => !Enum.IsDefined(status)))
        {
            throw new ArgumentOutOfRangeException(nameof(query.Statuses), "Payment request status filter contains an unsupported value.");
        }
    }

    private static PaymentRequestHistoryPage Empty(PaymentRequestPageRequest page) =>
        new([], page.PageNumber, page.PageSize, 0, false);
}
