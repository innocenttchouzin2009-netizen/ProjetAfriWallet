using AfriWallet.Merchants.Registry.Domain.Merchants;
using AfriWallet.Merchants.Registry.Domain.Profiles;

namespace AfriWallet.Merchants.Registry.Api.Contracts;

public sealed record CreateMerchantRequest(
    string OwnerAwid,
    string LegalName,
    string TradingName,
    MerchantType MerchantType,
    string CountryCode,
    string SettlementCurrency,
    string BusinessCategory,
    string? RegistrationNumber,
    string? TaxNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string PostalCode,
    string Email,
    string? Phone);

public sealed record SetCapabilitiesRequest(IReadOnlyCollection<MerchantCapability> Capabilities);

public sealed record ChangeMerchantStatusRequest(MerchantStatus Status);
