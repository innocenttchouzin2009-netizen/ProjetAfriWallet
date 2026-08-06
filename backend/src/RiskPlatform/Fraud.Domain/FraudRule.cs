namespace Fraud.Domain;

public sealed class FraudRule
{
    public string Id { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public int ScoreDelta { get; init; }
}
