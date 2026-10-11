namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionBinding
{
    private MobileMoneyPayoutExecutionBinding(
        Guid bindingId,
        MobileMoneyPayoutExecutionQuoteBinding quote,
        MobileMoneyPayoutExecutionFundingBinding funding,
        MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
        DateTimeOffset createdAtUtc)
    {
        BindingId = bindingId;
        Quote = quote;
        Funding = funding;
        IdempotencyKey = idempotencyKey;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid BindingId { get; }
    public MobileMoneyPayoutExecutionQuoteBinding Quote { get; }
    public MobileMoneyPayoutExecutionFundingBinding Funding { get; }
    public MobileMoneyPayoutExecutionIdempotencyKey IdempotencyKey { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public static MobileMoneyPayoutExecutionBinding Create(
        MobileMoneyPayoutExecutionQuoteBinding quote,
        MobileMoneyPayoutExecutionFundingBinding funding,
        MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
        DateTimeOffset createdAtUtc) =>
        CreateCore(
            Guid.NewGuid(),
            quote,
            funding,
            idempotencyKey,
            createdAtUtc);

    public static MobileMoneyPayoutExecutionBinding Restore(
        Guid bindingId,
        MobileMoneyPayoutExecutionQuoteBinding quote,
        MobileMoneyPayoutExecutionFundingBinding funding,
        MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
        DateTimeOffset createdAtUtc) =>
        CreateCore(
            bindingId,
            quote,
            funding,
            idempotencyKey,
            createdAtUtc);

    private static MobileMoneyPayoutExecutionBinding CreateCore(
        Guid bindingId,
        MobileMoneyPayoutExecutionQuoteBinding quote,
        MobileMoneyPayoutExecutionFundingBinding funding,
        MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
        DateTimeOffset createdAtUtc)
    {
        if (bindingId == Guid.Empty)
            throw new ArgumentException("Binding id cannot be empty.", nameof(bindingId));
        ArgumentNullException.ThrowIfNull(quote);
        ArgumentNullException.ThrowIfNull(funding);
        ArgumentNullException.ThrowIfNull(idempotencyKey);

        if (createdAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Binding timestamp must be UTC.", nameof(createdAtUtc));
        if (quote.ExpiresAtUtc <= createdAtUtc)
            throw new InvalidOperationException("Expired payout quote cannot be bound for execution.");
        if (!string.Equals(
                quote.SourceCurrency,
                funding.CurrencyCode,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Payout quote and funding plan currencies must match.");
        }
        if (quote.TotalSourceDebitMinor != funding.RequiredAmountMinor)
        {
            throw new InvalidOperationException(
                "Funding plan must exactly cover the quote total source debit.");
        }

        return new MobileMoneyPayoutExecutionBinding(
            bindingId,
            quote,
            funding,
            idempotencyKey,
            createdAtUtc);
    }
}
