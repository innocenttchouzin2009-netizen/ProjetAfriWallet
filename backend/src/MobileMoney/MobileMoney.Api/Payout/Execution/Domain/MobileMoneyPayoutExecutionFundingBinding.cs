namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionFundingBinding
{
    private MobileMoneyPayoutExecutionFundingBinding(
        Guid correlationId,
        long requiredAmountMinor,
        string currencyCode,
        IReadOnlyList<MobileMoneyPayoutExecutionFundingAllocation> allocations)
    {
        CorrelationId = correlationId;
        RequiredAmountMinor = requiredAmountMinor;
        CurrencyCode = currencyCode;
        Allocations = allocations;
    }

    public Guid CorrelationId { get; }
    public long RequiredAmountMinor { get; }
    public string CurrencyCode { get; }
    public IReadOnlyList<MobileMoneyPayoutExecutionFundingAllocation> Allocations { get; }

    public static MobileMoneyPayoutExecutionFundingBinding Create(
        Guid correlationId,
        long requiredAmountMinor,
        string currencyCode,
        IEnumerable<MobileMoneyPayoutExecutionFundingAllocation> allocations)
    {
        if (correlationId == Guid.Empty)
            throw new ArgumentException("Funding correlation id cannot be empty.", nameof(correlationId));
        if (requiredAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(requiredAmountMinor));
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Funding currency is required.", nameof(currencyCode));
        if (allocations is null)
            throw new ArgumentNullException(nameof(allocations));

        var normalizedCurrency = currencyCode.Trim().ToUpperInvariant();
        var materialized = allocations.ToArray();

        if (materialized.Length == 0)
            throw new ArgumentException("At least one funding allocation is required.", nameof(allocations));
        if (materialized.Any(x => x is null))
            throw new ArgumentException("Funding allocations cannot contain null entries.", nameof(allocations));
        if (materialized.Any(x => !string.Equals(
                x.CurrencyCode,
                normalizedCurrency,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "All funding allocations must use the funding currency.",
                nameof(allocations));
        }

        var allocatedMinor = materialized.Aggregate(
            0L,
            (total, allocation) => checked(total + allocation.AmountMinor));

        if (allocatedMinor != requiredAmountMinor)
        {
            throw new ArgumentException(
                "Funding allocations must exactly cover the required amount.",
                nameof(allocations));
        }

        return new MobileMoneyPayoutExecutionFundingBinding(
            correlationId,
            requiredAmountMinor,
            normalizedCurrency,
            Array.AsReadOnly(materialized));
    }
}
