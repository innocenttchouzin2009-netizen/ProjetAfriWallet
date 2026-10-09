using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Abstractions;

public sealed record MobileMoneyPayoutCreateOrGetResult(
    MobileMoneyPayout Payout,
    RequestFingerprint StoredFingerprint,
    bool Created)
{
    public bool IsReplay => !Created;

    public bool Matches(RequestFingerprint requestedFingerprint) =>
        StoredFingerprint == requestedFingerprint;
}
