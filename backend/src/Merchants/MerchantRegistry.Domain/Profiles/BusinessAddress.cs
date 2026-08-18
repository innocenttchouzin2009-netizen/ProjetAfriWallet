namespace AfriWallet.Merchants.Registry.Domain.Profiles;

public sealed record BusinessAddress
{
    public BusinessAddress(string addressLine1, string? addressLine2, string city, string postalCode, string countryCode)
    {
        if (string.IsNullOrWhiteSpace(addressLine1))
            throw new ArgumentException("Address line 1 is required.", nameof(addressLine1));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));
        if (string.IsNullOrWhiteSpace(countryCode))
            throw new ArgumentException("Country code is required.", nameof(countryCode));
        if (countryCode.Trim().Length != 2)
            throw new ArgumentException("Country code must be ISO-3166 alpha-2.", nameof(countryCode));

        AddressLine1 = addressLine1.Trim();
        AddressLine2 = addressLine2?.Trim();
        City = city.Trim();
        PostalCode = postalCode?.Trim() ?? string.Empty;
        CountryCode = countryCode.Trim().ToUpperInvariant();
    }

    public string AddressLine1 { get; }
    public string? AddressLine2 { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string CountryCode { get; }
}
