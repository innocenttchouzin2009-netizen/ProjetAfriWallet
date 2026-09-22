namespace Settlement.Infrastructure.Gateways;

public sealed record SettlementFxClearingAccountPair(
    string SourceCurrency,
    Guid SourceClearingAccountId,
    string DestinationCurrency,
    Guid DestinationClearingAccountId);

public interface ISettlementFxClearingAccountResolver
{
    SettlementFxClearingAccountPair Resolve(string sourceCurrency, string destinationCurrency);
}

public sealed class ConfiguredSettlementFxClearingAccountResolver(
    IEnumerable<SettlementFxClearingAccountPair> pairs)
    : ISettlementFxClearingAccountResolver
{
    private readonly IReadOnlyDictionary<string, SettlementFxClearingAccountPair> byPair =
        pairs.ToDictionary(
            x => Key(x.SourceCurrency, x.DestinationCurrency),
            x => x,
            StringComparer.Ordinal);

    public SettlementFxClearingAccountPair Resolve(string sourceCurrency, string destinationCurrency)
    {
        var key = Key(sourceCurrency, destinationCurrency);
        if (!byPair.TryGetValue(key, out var pair))
            throw new InvalidOperationException($"No FX clearing accounts are configured for {key}.");
        return pair;
    }

    private static string Key(string sourceCurrency, string destinationCurrency) =>
        $"{sourceCurrency.Trim().ToUpperInvariant()}/{destinationCurrency.Trim().ToUpperInvariant()}";
}
