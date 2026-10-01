namespace AfriWallet.TransactionRead.Application.Contracts;

public readonly record struct TransactionReadCursor
{
    public DateTimeOffset OccurredAtUtc { get; }
    public Guid TransactionId { get; }

    public TransactionReadCursor(
        DateTimeOffset occurredAtUtc,
        Guid transactionId)
    {
        if (occurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Transaction read cursor timestamp must be UTC.",
                nameof(occurredAtUtc));
        }

        if (transactionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Transaction read cursor transaction id cannot be empty.",
                nameof(transactionId));
        }

        OccurredAtUtc = occurredAtUtc;
        TransactionId = transactionId;
    }
}
