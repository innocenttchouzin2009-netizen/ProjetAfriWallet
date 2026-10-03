namespace MobileMoney.Production.Payout.Domain;

public sealed record MobileMoneyBeneficiary
{
    public MobileMoneyBeneficiary(
        string msisdn,
        string countryCode,
        string operatorCode,
        string? displayName = null)
    {
        Msisdn = NormalizeMsisdn(msisdn);
        CountryCode = NormalizeCountryCode(countryCode);
        OperatorCode = NormalizeOperatorCode(operatorCode);
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    }

    public string Msisdn { get; }
    public string CountryCode { get; }
    public string OperatorCode { get; }
    public string? DisplayName { get; }

    private static string NormalizeMsisdn(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Beneficiary MSISDN is required.", nameof(value));

        var normalized = value.Trim();
        if (!normalized.StartsWith('+') ||
            normalized.Length is < 9 or > 16 ||
            normalized[1..].Any(ch => ch is < '0' or > '9'))
        {
            throw new ArgumentException(
                "Beneficiary MSISDN must use E.164 format.",
                nameof(value));
        }

        return normalized;
    }

    private static string NormalizeCountryCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Beneficiary country code is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 2 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw new ArgumentException(
                "Beneficiary country code must contain two ISO-like letters.",
                nameof(value));

        return normalized;
    }

    private static string NormalizeOperatorCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Mobile money operator code is required.", nameof(value));

        return value.Trim().ToUpperInvariant();
    }
}
