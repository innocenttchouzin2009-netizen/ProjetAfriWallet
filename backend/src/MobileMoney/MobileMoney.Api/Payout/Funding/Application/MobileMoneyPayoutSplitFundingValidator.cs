using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Application;

public sealed class MobileMoneyPayoutSplitFundingValidator
{
    public MobileMoneyPayoutFundingPlan ValidateAndPlan(
        PlanMobileMoneyPayoutFundingCommand command,
        IReadOnlyCollection<FundingSourceSnapshot> sources)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(sources);

        if (command.CorrelationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id cannot be empty.", nameof(command));
        }

        if (command.RequiredAmountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                "Required funding amount must be positive.");
        }

        if (command.RequestedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Funding request timestamp must be UTC.",
                nameof(command));
        }

        var currencyCode = NormalizeCurrency(command.CurrencyCode);
        if (command.Allocations is null || command.Allocations.Count == 0)
        {
            throw new ArgumentException(
                "At least one funding allocation is required.",
                nameof(command));
        }

        var sourcesById = BuildSourcesById(sources);
        var seenSourceIds = new HashSet<string>(StringComparer.Ordinal);
        long allocatedMinor = 0;

        foreach (var allocation in command.Allocations)
        {
            ArgumentNullException.ThrowIfNull(allocation);

            if (!seenSourceIds.Add(allocation.SourceId))
            {
                throw new InvalidOperationException(
                    $"Funding source '{allocation.SourceId}' cannot be allocated more than once.");
            }

            if (!string.Equals(
                    allocation.CurrencyCode,
                    currencyCode,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Every funding allocation must use the payout funding currency.");
            }

            if (!sourcesById.TryGetValue(allocation.SourceId, out var source))
            {
                throw new InvalidOperationException(
                    $"Funding source '{allocation.SourceId}' is not available.");
            }

            ValidateSourceMatchesAllocation(source, allocation, currencyCode);
            allocatedMinor = checked(allocatedMinor + allocation.AmountMinor);
        }

        if (allocatedMinor != command.RequiredAmountMinor)
        {
            throw new InvalidOperationException(
                "Funding allocations must sum exactly to the required funding amount.");
        }

        return new MobileMoneyPayoutFundingPlan(
            command.CorrelationId,
            command.RequiredAmountMinor,
            currencyCode,
            command.Allocations.ToArray(),
            command.RequestedAtUtc);
    }

    private static Dictionary<string, FundingSourceSnapshot> BuildSourcesById(
        IReadOnlyCollection<FundingSourceSnapshot> sources)
    {
        var sourcesById =
            new Dictionary<string, FundingSourceSnapshot>(StringComparer.Ordinal);

        foreach (var source in sources)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (string.IsNullOrWhiteSpace(source.SourceId))
            {
                throw new ArgumentException(
                    "Funding source id is required.",
                    nameof(sources));
            }

            if (!Enum.IsDefined(typeof(FundingSourceType), source.SourceType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sources),
                    "Funding source type is not supported.");
            }

            if (!sourcesById.TryAdd(source.SourceId.Trim(), source))
            {
                throw new InvalidOperationException(
                    $"Funding source '{source.SourceId}' is duplicated.");
            }
        }

        return sourcesById;
    }

    private static void ValidateSourceMatchesAllocation(
        FundingSourceSnapshot source,
        FundingAllocation allocation,
        string currencyCode)
    {
        if (source.SourceType != allocation.SourceType)
        {
            throw new InvalidOperationException(
                $"Funding source '{allocation.SourceId}' type does not match its allocation.");
        }

        if (!source.IsAvailable)
        {
            throw new InvalidOperationException(
                $"Funding source '{allocation.SourceId}' is unavailable.");
        }

        if (!string.Equals(
                NormalizeCurrency(source.CurrencyCode),
                currencyCode,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Funding source '{allocation.SourceId}' currency does not match the payout funding currency.");
        }

        if (source.SourceType == FundingSourceType.Wallet)
        {
            if (source.AvailableMinor is null || source.AvailableMinor < 0)
            {
                throw new InvalidOperationException(
                    $"Wallet funding source '{allocation.SourceId}' must expose a non-negative available balance.");
            }

            if (allocation.AmountMinor > source.AvailableMinor.Value)
            {
                throw new InvalidOperationException(
                    $"Wallet funding allocation '{allocation.SourceId}' exceeds available balance.");
            }
        }
    }

    private static string NormalizeCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException(
                "Currency code is required.",
                nameof(currencyCode));
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 ||
            normalized.Any(ch => ch is < 'A' or > 'Z'))
        {
            throw new ArgumentException(
                "Currency code must contain exactly three ISO-like letters.",
                nameof(currencyCode));
        }

        return normalized;
    }
}
