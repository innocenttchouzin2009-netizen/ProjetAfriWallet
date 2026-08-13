namespace BankRouting.Domain.Rails;

public sealed record BankRail(
    string RailId,
    BankRailType RailType,
    string CountryCode,
    string CurrencyCode,
    bool IsActive,
    bool IsHealthy,
    long MinAmountMinor,
    long MaxAmountMinor,
    int Priority,
    long EstimatedCostMinor,
    string? Description = null)
{
    public bool Supports(
        string countryCode,
        string currencyCode,
        long amountMinor)
    {
        if (!IsActive || !IsHealthy)
            return false;

        if (!string.Equals(countryCode, CountryCode, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals(currencyCode, CurrencyCode, StringComparison.OrdinalIgnoreCase))
            return false;

        if (amountMinor < MinAmountMinor || amountMinor > MaxAmountMinor)
            return false;

        return true;
    }
}
