namespace PaymentGateway.Api.Infrastructure;

public interface IConnectorResolver
{
    ConnectorResolution Resolve(string providerCode, string transferType);
}

public sealed record ConnectorResolution(string ProviderCode, string ConnectorType, string ExecutionMode);

public sealed class ConnectorResolver : IConnectorResolver
{
    public ConnectorResolution Resolve(string providerCode, string transferType)
    {
        return providerCode.ToUpperInvariant() switch
        {
            "MTN" => new ConnectorResolution("MTN", "MTN_MOMO", "Immediate"),
            "ORANGE" => new ConnectorResolution("ORANGE", "ORANGE_MONEY", "Immediate"),
            "BANK" => new ConnectorResolution("BANK", "SEPA", transferType.Equals("SWIFT", StringComparison.OrdinalIgnoreCase) ? "Deferred" : "Immediate"),
            "CARD" => new ConnectorResolution("CARD", "VISA_MASTERCARD", "Immediate"),
            _ => new ConnectorResolution(providerCode, "DEFAULT", "Immediate")
        };
    }
}
