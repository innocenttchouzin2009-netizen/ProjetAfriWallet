namespace MerchantSettlement.Domain.Profiles;

public sealed class MerchantSettlementProfile
{
    public MerchantSettlementProfile(
        Guid profileId,
        string merchantId,
        string settlementCurrency,
        SettlementFrequency frequency,
        int settlementDelayDays)
    {
        if (profileId == Guid.Empty)
            throw new ArgumentException("Settlement profile ID is required.");

        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant ID is required.");

        var currency = settlementCurrency.Trim().ToUpperInvariant();

        if (currency.Length != 3)
            throw new ArgumentException("Settlement currency must use ISO 4217.");

        if (settlementDelayDays is < 0 or > 30)
            throw new ArgumentOutOfRangeException(nameof(settlementDelayDays));

        ProfileId = profileId;
        MerchantId = merchantId.Trim();
        SettlementCurrency = currency;
        Frequency = frequency;
        SettlementDelayDays = settlementDelayDays;
    }

    public Guid ProfileId { get; }

    public string MerchantId { get; }

    public string SettlementCurrency { get; }

    public SettlementFrequency Frequency { get; private set; }

    public int SettlementDelayDays { get; private set; }

    public long MinimumSettlementMinor { get; private set; }

    public MerchantSettlementProfileStatus Status { get; private set; }
        = MerchantSettlementProfileStatus.Active;

    public DateTime CreatedAtUtc { get; }
        = DateTime.UtcNow;

    public void ConfigureMinimum(long minimumSettlementMinor)
    {
        if (minimumSettlementMinor < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumSettlementMinor));

        MinimumSettlementMinor = minimumSettlementMinor;
    }

    public void Suspend()
    {
        Status = MerchantSettlementProfileStatus.Suspended;
    }

    public void Activate()
    {
        Status = MerchantSettlementProfileStatus.Active;
    }
}

public enum SettlementFrequency
{
    Daily,
    Weekly,
    BiWeekly,
    Monthly,
    Manual
}

public enum MerchantSettlementProfileStatus
{
    Active,
    Suspended
}
