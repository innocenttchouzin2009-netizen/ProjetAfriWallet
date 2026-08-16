namespace AfriWallet.Compliance.Screening.Domain.Matching;

public sealed record ScreeningThresholds(
    double ReviewThreshold,
    double BlockThreshold)
{
    public static ScreeningThresholds Default =>
        new(
            ReviewThreshold: 0.70,
            BlockThreshold: 0.90);
}