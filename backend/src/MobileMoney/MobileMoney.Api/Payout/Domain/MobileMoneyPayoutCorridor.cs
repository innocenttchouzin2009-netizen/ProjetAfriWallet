namespace MobileMoney.Production.Payout.Domain;

public sealed record MobileMoneyPayoutCorridor
{
    public MobileMoneyPayoutCorridor(
        string sourceCountryCode,
        string sourceCurrency,
        string destinationCountryCode,
        string destinationCurrency,
        string operatorCode)
    {
        SourceCountryCode = NormalizeCountryCode(sourceCountryCode, nameof(sourceCountryCode));
        SourceCurrency = NormalizeCurrency(sourceCurrency, nameof(sourceCurrency));
        DestinationCountryCode = NormalizeCountryCode(destinationCountryCode, nameof(destinationCountryCode));
        DestinationCurrency = NormalizeCurrency(destinationCurrency, nameof(destinationCurrency));
        OperatorCode = NormalizeOperatorCode(operatorCode);
    }

    public string SourceCountryCode { get; }
    public string SourceCurrency { get; }
    public string DestinationCountryCode { get; }
    public string DestinationCurrency { get; }
    public string OperatorCode { get; }

    private static string NormalizeCountryCode(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Country code is required.", parameterName);

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 2 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw new ArgumentException("Country code must contain two ISO-like letters.", parameterName);

        return normalized;
    }

    private static string NormalizeCurrency(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency is required.", parameterName);

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must contain three ISO-like letters.", parameterName);

        return normalized;
    }

    private static string NormalizeOperatorCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Mobile money operator code is required.", nameof(value));

        return value.Trim().ToUpperInvariant();
    }
}
