namespace UniversalWallet.Api.Payments.Domain.MerchantPayments;

public enum MerchantStatus
{
    Pending,
    Active,
    Suspended,
    Closed
}

public enum MerchantVerificationLevel
{
    Unverified,
    Verified
}

public enum MerchantCategoryCode
{
    Restaurant,
    Grocery,
    Fashion,
    Transport,
    Healthcare,
    Education,
    Services,
    OnlineRetail,
    Other
}

public sealed class MerchantProfile
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid OwnerAwidId { get; init; }
    public Guid MerchantAwid { get; init; }
    public string BusinessName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public MerchantCategoryCode CategoryCode { get; set; } = MerchantCategoryCode.Other;
    public Guid SettlementWalletId { get; set; }
    public MerchantStatus Status { get; set; } = MerchantStatus.Pending;
    public MerchantVerificationLevel VerificationLevel { get; set; } = MerchantVerificationLevel.Unverified;
    public string CountryCode { get; set; } = "CM";
    public string DefaultCurrency { get; set; } = "XAF";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ActivatedAt { get; set; }
    public int Version { get; set; } = 1;
}
