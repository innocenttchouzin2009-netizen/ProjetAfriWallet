namespace AfriWallet.Compliance.RiskScoring.Domain.Factors;

public sealed record RiskFactor(
    string Code,
    RiskFactorType Type,
    string Description,
    int Weight,
    bool Enabled);