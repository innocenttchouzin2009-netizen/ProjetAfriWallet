namespace MobileMoney.Production.Payout.Domain;

public sealed record MobileMoneyPayoutCorridorCapability(
    MobileMoneyPayoutCorridor Corridor,
    bool Enabled,
    bool CollectionEnabled,
    bool OutboundPayoutEnabled);
