using AfriWallet.TransactionRead.Application.Abstractions;
using AfriWallet.TransactionRead.Application.Contracts;
using AfriWallet.TransactionRead.Application.Projection;

namespace AfriWallet.TransactionRead.Application;

public sealed class TransactionReadQueryService(
    ITransactionLedgerReader ledgerReader,
    ITransactionReadWalletAccessReader walletAccessReader)
{
    public async Task<TransactionReadPage> ReadAsync(
        Guid authenticatedUserId,
        TransactionReadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (authenticatedUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Authenticated user id cannot be empty.",
                nameof(authenticatedUserId));
        }

        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var walletAccountId =
            await walletAccessReader.ResolveOwnedLedgerAccountAsync(
                authenticatedUserId,
                request.WalletId,
                cancellationToken);

        if (walletAccountId is null)
        {
            throw new UnauthorizedAccessException(
                "The requested wallet is not accessible by the authenticated user.");
        }

        var take = checked(request.Limit + 1);
        var journals = await ledgerReader.ReadAsync(
            walletAccountId.Value,
            request.Cursor,
            take,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Transaction ledger reader returned null.");

        if (journals.Count > take)
        {
            throw new InvalidOperationException(
                "Transaction ledger reader returned more journals than requested.");
        }

        var projected = new List<TransactionReadItem>(journals.Count);

        foreach (var journal in journals)
        {
            var item = LedgerTransactionProjector.Project(
                request.WalletId,
                walletAccountId.Value,
                journal);

            if (item is not null)
            {
                projected.Add(item);
            }
        }

        if (projected.Count > request.Limit)
        {
            var items = projected
                .Take(request.Limit)
                .ToArray();

            var lastReturned = items[^1];

            return new TransactionReadPage(
                items,
                new TransactionReadCursor(
                    lastReturned.OccurredAtUtc,
                    lastReturned.TransactionId));
        }

        TransactionReadCursor? nextCursor = null;

        if (journals.Count == take && journals.Count > 0)
        {
            var lastConsumed = journals[^1];
            nextCursor = new TransactionReadCursor(
                lastConsumed.PostedAtUtc,
                lastConsumed.Id.Value);
        }

        return new TransactionReadPage(
            projected.ToArray(),
            nextCursor);
    }
}
