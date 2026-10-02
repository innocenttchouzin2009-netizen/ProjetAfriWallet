using AfriWallet.TransactionHistory.Application.Abstractions;
using AfriWallet.TransactionHistory.Application.Contracts;
using AfriWallet.TransactionHistory.Application.Cursor;

namespace AfriWallet.TransactionHistory.Application;

public sealed class AuthorizedTransactionHistoryQueryService(
    ITransactionHistoryReader historyReader,
    ITransactionHistoryOwnedWalletReader ownedWalletReader)
{
    public async Task<TransactionHistoryPage> ListAsync(
        TransactionHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        Validate(query);
        cancellationToken.ThrowIfCancellationRequested();

        var wallets = await ownedWalletReader.ListOwnedWalletIdsAsync(query.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Owned wallet reader returned null.");

        var authorizedWallets = wallets
            .Where(wallet => wallet.Value != Guid.Empty)
            .Distinct()
            .ToArray();

        if (authorizedWallets.Length == 0)
        {
            return new TransactionHistoryPage([], null);
        }

        return await historyReader.ReadAsync(
            authorizedWallets,
            query.Page,
            cancellationToken);
    }

    private static void Validate(TransactionHistoryQuery query)
    {
        if (query.UserId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user id cannot be empty.", nameof(query));
        }

        ArgumentNullException.ThrowIfNull(query.Page);
        _ = TransactionHistoryPageRequest.Create(query.Page.Limit, query.Page.Cursor);
    }
}
