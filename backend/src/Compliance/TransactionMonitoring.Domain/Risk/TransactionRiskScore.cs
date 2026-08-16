namespace AfriWallet.Compliance.TransactionMonitoring.Domain.Risk;

public sealed record TransactionRiskScore(
    int Score,
    string Band)
{
    public static TransactionRiskScore FromPoints(int points)
    {
        var score = Math.Clamp(points, 0, 100);
        var band = score switch
        {
            >= 80 => "CRITICAL",
            >= 60 => "HIGH",
            >= 30 => "MEDIUM",
            _ => "LOW"
        };

        return new TransactionRiskScore(score, band);
    }
}