namespace AfriWallet.TransactionHistory.Application.Cursor;

public readonly record struct TransactionHistoryCursor
{
    public DateTimeOffset OccurredAtUtc { get; }
    public Guid TransactionId { get; }

    public TransactionHistoryCursor(
        DateTimeOffset occurredAtUtc,
        Guid transactionId)
    {
        if (occurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Transaction history cursor timestamp must be UTC.", nameof(occurredAtUtc));
        }

        if (transactionId == Guid.Empty)
        {
            throw new ArgumentException("Transaction history cursor transaction id cannot be empty.", nameof(transactionId));
        }

        OccurredAtUtc = occurredAtUtc;
        TransactionId = transactionId;
    }
}
