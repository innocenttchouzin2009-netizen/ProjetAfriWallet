namespace AfriWallet.Compliance.RiskScoring.Domain.Scores;

public sealed record RiskScore(int Value)
{
    public static RiskScore Create(int value) => new(Math.Clamp(value, 0, 100));
}