using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed class PaymentRequestHistoryService(
    IPaymentRequestHistoryReader historyReader,
    IPaymentRequestOwnedWalletReader ownedWalletReader,
    IPaymentRequestOwnedRecipientReferenceReader ownedRecipientReferenceReader)
{
    public async Task<PaymentRequestHistoryPage> ListAsync(
        PaymentRequestHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.Page);

        if (query.UserId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user id cannot be empty.", nameof(query.UserId));
        }

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
            throw new ArgumentOutOfRangeException(nameof(query.Statuses));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var wallets = await ownedWalletReader.ListOwnedWalletIdsAsync(query.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Owned wallet reader returned null.");
        var references = await ownedRecipientReferenceReader.ListOwnedRecipientReferencesAsync(query.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Owned recipient reference reader returned null.");

        var ownedWalletIds = wallets
            .Where(wallet => wallet.Value != Guid.Empty)
            .Distinct()
            .ToArray();
        var ownedRecipientReferences = references
            .Where(reference => reference is not null)
            .Distinct()
            .ToArray();

        return await historyReader.ListAsync(
            new PaymentRequestHistoryReadQuery(
                ownedWalletIds,
                ownedRecipientReferences,
                query.Direction,
                query.Statuses,
                query.Page,
                query.Order),
            cancellationToken);
    }
}
