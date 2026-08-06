namespace Fraud.Domain;

public sealed class FraudSignal
{
    public string Source { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
