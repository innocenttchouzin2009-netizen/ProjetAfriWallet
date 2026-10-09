using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Infrastructure;

public sealed class ConfiguredMobileMoneyPayoutFundingSourceReader
    : IMobileMoneyPayoutFundingSourceReader
{
    public const string SectionName = "MobileMoney:Payout:Funding:Sources";

    private readonly IReadOnlyDictionary<
        (string SourceId, FundingSourceType SourceType),
        FundingSourceSnapshot> _sources;

    public ConfiguredMobileMoneyPayoutFundingSourceReader(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _sources = configuration
            .GetSection(SectionName)
            .GetChildren()
            .Select(ToSnapshot)
            .ToDictionary(
                snapshot => (snapshot.SourceId, snapshot.SourceType));
    }

    public Task<FundingSourceSnapshot?> GetAsync(
        string sourceId,
        FundingSourceType sourceType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return Task.FromResult<FundingSourceSnapshot?>(null);
        }

        _sources.TryGetValue(
            (sourceId.Trim(), sourceType),
            out var snapshot);

        return Task.FromResult(snapshot);
    }

    private static FundingSourceSnapshot ToSnapshot(
        IConfigurationSection section)
    {
        var sourceId = section["SourceId"];
        var sourceTypeText = section["SourceType"];
        var currencyCode = section["CurrencyCode"];

        if (string.IsNullOrWhiteSpace(sourceId)
            || string.IsNullOrWhiteSpace(currencyCode)
            || !Enum.TryParse<FundingSourceType>(
                sourceTypeText,
                ignoreCase: true,
                out var sourceType))
        {
            throw new InvalidOperationException(
                $"Each {SectionName} entry must define SourceId, SourceType and CurrencyCode.");
        }

        var isAvailable = bool.TryParse(
            section["IsAvailable"],
            out var parsedAvailability)
            && parsedAvailability;

        long? availableMinor = null;
        var availableMinorText = section["AvailableMinor"];
        if (!string.IsNullOrWhiteSpace(availableMinorText))
        {
            if (!long.TryParse(availableMinorText, out var parsedAvailableMinor))
            {
                throw new InvalidOperationException(
                    $"Funding source '{sourceId}' has an invalid AvailableMinor value.");
            }

            availableMinor = parsedAvailableMinor;
        }

        return new FundingSourceSnapshot(
            sourceId.Trim(),
            sourceType,
            currencyCode.Trim(),
            isAvailable,
            availableMinor);
    }
}
