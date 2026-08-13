namespace Liquidity.Domain.Forecast;

public sealed record LiquidityForecast(
    string CurrencyCode,
    long EstimatedNetJPlus1Minor,
    long EstimatedNetJPlus7Minor,
    long EstimatedNetJPlus30Minor,
    DateTime CalculatedAtUtc);
