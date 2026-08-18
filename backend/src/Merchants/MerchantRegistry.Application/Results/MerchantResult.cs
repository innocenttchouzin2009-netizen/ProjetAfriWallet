using AfriWallet.Merchants.Registry.Domain.Merchants;
using AfriWallet.Merchants.Registry.Domain.Profiles;

namespace AfriWallet.Merchants.Registry.Application.Results;

public sealed record MerchantResult(
    string MerchantId,
    string OwnerAwid,
    MerchantStatus Status,
    BusinessProfile Profile,
    IReadOnlyCollection<MerchantCapability> Capabilities,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
