namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionIdempotencyEntry
{
    public MobileMoneyPayoutExecutionIdempotencyEntry(
        string idempotencyKey,
        string fingerprint,
        Guid quoteId,
        Guid? payoutId,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException(
                "Idempotency key is required.",
                nameof(idempotencyKey));
        if (string.IsNullOrWhiteSpace(fingerprint))
            throw new ArgumentException(
                "Fingerprint is required.",
                nameof(fingerprint));
        if (quoteId == Guid.Empty)
            throw new ArgumentException("Quote id is required.", nameof(quoteId));
        if (payoutId == Guid.Empty)
            throw new ArgumentException(
                "Payout id cannot be empty when supplied.",
                nameof(payoutId));
        if (createdAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException(
                "Timestamp must be UTC.",
                nameof(createdAtUtc));

        IdempotencyKey = idempotencyKey.Trim();
        Fingerprint = fingerprint.Trim();
        QuoteId = quoteId;
        PayoutId = payoutId;
        CreatedAtUtc = createdAtUtc;
    }

    public string IdempotencyKey { get; }
    public string Fingerprint { get; }
    public Guid QuoteId { get; }
    public Guid? PayoutId { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public bool Matches(MobileMoneyPayoutExecutionIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        return string.Equals(
                IdempotencyKey,
                intent.IdempotencyKey,
                StringComparison.Ordinal)
            && string.Equals(
                Fingerprint,
                intent.Fingerprint,
                StringComparison.Ordinal);
    }
}
