namespace AfriWallet.Compliance.RiskScoring.Domain.Factors;

public sealed record RiskFactorContribution(
    string FactorCode,
    RiskFactorType Type,
    int RawScore,
    int Weight,
    int WeightedScore,
    string Reason);