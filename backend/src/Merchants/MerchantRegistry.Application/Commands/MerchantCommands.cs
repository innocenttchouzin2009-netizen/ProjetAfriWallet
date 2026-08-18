using AfriWallet.Merchants.Registry.Domain.Merchants;
using AfriWallet.Merchants.Registry.Domain.Profiles;

namespace AfriWallet.Merchants.Registry.Application.Commands;

public sealed record CreateMerchantCommand(string OwnerAwid, BusinessProfile Profile, string Actor);
public sealed record RegisterMerchantCommand(string MerchantId, string Actor);
public sealed record SetMerchantCapabilitiesCommand(string MerchantId, IReadOnlyCollection<MerchantCapability> Capabilities, string Actor);
public sealed record UpdateMerchantProfileCommand(string MerchantId, BusinessProfile Profile, string Actor);
public sealed record ChangeMerchantStatusCommand(string MerchantId, MerchantStatus TargetStatus, string Actor);
