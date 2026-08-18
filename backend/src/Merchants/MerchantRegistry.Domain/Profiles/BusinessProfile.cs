using AfriWallet.Merchants.Registry.Domain.Merchants;

namespace AfriWallet.Merchants.Registry.Domain.Profiles;

public sealed record BusinessProfile
{
    public BusinessProfile(
        string legalName,
        string tradingName,
        MerchantType merchantType,
        string countryCode,
        string settlementCurrency,
        string businessCategory,
        string? registrationNumber,
        string? taxNumber,
        BusinessAddress address,
        MerchantContact contact)
    {
        if (string.IsNullOrWhiteSpace(legalName))
            throw new ArgumentException("Legal name is required.", nameof(legalName));
        if (string.IsNullOrWhiteSpace(tradingName))
            throw new ArgumentException("Trading name is required.", nameof(tradingName));
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            throw new ArgumentException("Country code must be ISO-3166 alpha-2.", nameof(countryCode));
        if (string.IsNullOrWhiteSpace(settlementCurrency) || settlementCurrency.Trim().Length != 3)
            throw new ArgumentException("Settlement currency must be ISO-4217 style.", nameof(settlementCurrency));
        if (string.IsNullOrWhiteSpace(businessCategory))
            throw new ArgumentException("Business category is required.", nameof(businessCategory));
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(contact);

        LegalName = legalName.Trim();
        TradingName = tradingName.Trim();
        MerchantType = merchantType;
        CountryCode = countryCode.Trim().ToUpperInvariant();
        SettlementCurrency = settlementCurrency.Trim().ToUpperInvariant();
        BusinessCategory = businessCategory.Trim();
        RegistrationNumber = registrationNumber?.Trim();
        TaxNumber = taxNumber?.Trim();
        Address = address;
        Contact = contact;
    }

    public string LegalName { get; }
    public string TradingName { get; }
    public MerchantType MerchantType { get; }
    public string CountryCode { get; }
    public string SettlementCurrency { get; }
    public string BusinessCategory { get; }
    public string? RegistrationNumber { get; }
    public string? TaxNumber { get; }
    public BusinessAddress Address { get; }
    public MerchantContact Contact { get; }
}
