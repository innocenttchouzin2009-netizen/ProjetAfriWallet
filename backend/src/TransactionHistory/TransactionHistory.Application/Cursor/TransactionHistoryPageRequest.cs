namespace AfriWallet.TransactionHistory.Application.Cursor;

public sealed record TransactionHistoryPageRequest
{
    public const int DefaultLimit = 50;
    public const int MaximumLimit = 100;

    public int Limit { get; }
    public TransactionHistoryCursor? Cursor { get; }

    private TransactionHistoryPageRequest(
        int limit,
        TransactionHistoryCursor? cursor)
    {
        Limit = limit;
        Cursor = cursor;
    }

    public static TransactionHistoryPageRequest Create(
        int limit = DefaultLimit,
        TransactionHistoryCursor? cursor = null)
    {
        if (limit is < 1 or > MaximumLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                $"Transaction history page size must be between 1 and {MaximumLimit}.");
        }

        return new TransactionHistoryPageRequest(limit, cursor);
    }
}
