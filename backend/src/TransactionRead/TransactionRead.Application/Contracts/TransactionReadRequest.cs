using AfriWallet.Wallet.Domain;

namespace AfriWallet.TransactionRead.Application.Contracts;

public sealed record TransactionReadRequest
{
    public const int DefaultLimit = 50;
    public const int MaximumLimit = 100;

    public WalletId WalletId { get; }
    public int Limit { get; }
    public TransactionReadCursor? Cursor { get; }

    private TransactionReadRequest(
        WalletId walletId,
        int limit,
        TransactionReadCursor? cursor)
    {
        WalletId = walletId;
        Limit = limit;
        Cursor = cursor;
    }

    public static TransactionReadRequest Create(
        WalletId walletId,
        int limit = DefaultLimit,
        TransactionReadCursor? cursor = null)
    {
        if (walletId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Wallet id cannot be empty.",
                nameof(walletId));
        }

        if (limit is < 1 or > MaximumLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                $"Transaction read page size must be between 1 and {MaximumLimit}.");
        }

        return new TransactionReadRequest(walletId, limit, cursor);
    }
}
