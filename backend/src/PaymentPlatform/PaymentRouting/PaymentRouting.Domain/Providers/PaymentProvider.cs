using PaymentRouting.Domain.Routes;

namespace PaymentRouting.Domain.Providers;

public sealed class PaymentProvider
{
    public PaymentProvider(
        string providerId,
        string displayName,
        PaymentRail rail,
        IReadOnlyCollection<string> countries,
        IReadOnlyCollection<string> currencies,
        decimal baseCostScore,
        int priority)
    {
        ProviderId = Require(providerId);
        DisplayName = Require(displayName);
        Rail = rail;

        Countries = countries
            .Select(x => x.ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Currencies = currencies
            .Select(x => x.ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (baseCostScore < 0)
            throw new ArgumentOutOfRangeException(
                nameof(baseCostScore));

        if (priority < 0)
            throw new ArgumentOutOfRangeException(
                nameof(priority));

        BaseCostScore = baseCostScore;
        Priority = priority;
    }

    public string ProviderId { get; }

    public string DisplayName { get; }

    public PaymentRail Rail { get; }

    public IReadOnlyCollection<string> Countries { get; }

    public IReadOnlyCollection<string> Currencies { get; }

    public decimal BaseCostScore { get; }

    public int Priority { get; }

    public ProviderStatus Status { get; private set; }
        = ProviderStatus.Active;

    public double SuccessRate { get; private set; } = 1.0;

    public double AverageLatencyMs { get; private set; } = 100;

    public DateTime UpdatedAtUtc { get; private set; }
        = DateTime.UtcNow;

    public void UpdateHealth(
        ProviderStatus status,
        double successRate,
        double averageLatencyMs)
    {
        if (successRate is < 0 or > 1)
            throw new ArgumentOutOfRangeException(
                nameof(successRate));

        if (averageLatencyMs < 0)
            throw new ArgumentOutOfRangeException(
                nameof(averageLatencyMs));

        Status = status;
        SuccessRate = successRate;
        AverageLatencyMs = averageLatencyMs;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool Supports(
        string countryCode,
        string currencyCode)
    {
        return Countries.Contains(
                   countryCode.ToUpperInvariant(),
                   StringComparer.OrdinalIgnoreCase)
               &&
               Currencies.Contains(
                   currencyCode.ToUpperInvariant(),
                   StringComparer.OrdinalIgnoreCase);
    }

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Value is required.");

        return value.Trim();
    }
}
