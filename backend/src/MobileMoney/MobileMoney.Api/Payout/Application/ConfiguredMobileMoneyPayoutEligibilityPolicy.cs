using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Application;

public sealed class ConfiguredMobileMoneyPayoutEligibilityPolicy
    : IMobileMoneyPayoutEligibilityPolicy
{
    private readonly IReadOnlyList<MobileMoneyPayoutCorridorCapability> _capabilities;

    public ConfiguredMobileMoneyPayoutEligibilityPolicy(
        IEnumerable<MobileMoneyPayoutCorridorCapability> capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        _capabilities = capabilities.ToArray();

        if (_capabilities.Any(capability => capability.Corridor is null))
            throw new ArgumentException("Corridor capability cannot contain a null corridor.", nameof(capabilities));
    }

    public Task<MobileMoneyPayoutEligibilityResult> EvaluateAsync(
        MobileMoneyPayoutCorridor corridor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(corridor);
        cancellationToken.ThrowIfCancellationRequested();

        var routeCapabilities = _capabilities
            .Where(capability => SameRoute(capability.Corridor, corridor))
            .ToArray();

        if (routeCapabilities.Length == 0)
        {
            return Task.FromResult(
                MobileMoneyPayoutEligibilityResult.Ineligible(
                    MobileMoneyPayoutEligibilityCodes.CorridorNotSupported));
        }

        var operatorCapability = routeCapabilities.FirstOrDefault(
            capability => string.Equals(
                capability.Corridor.OperatorCode,
                corridor.OperatorCode,
                StringComparison.OrdinalIgnoreCase));

        if (operatorCapability is null)
        {
            return Task.FromResult(
                MobileMoneyPayoutEligibilityResult.Ineligible(
                    MobileMoneyPayoutEligibilityCodes.OperatorNotActivated));
        }

        if (!operatorCapability.Enabled)
        {
            return Task.FromResult(
                MobileMoneyPayoutEligibilityResult.Ineligible(
                    MobileMoneyPayoutEligibilityCodes.CorridorDisabled));
        }

        if (!operatorCapability.OutboundPayoutEnabled)
        {
            return Task.FromResult(
                MobileMoneyPayoutEligibilityResult.Ineligible(
                    MobileMoneyPayoutEligibilityCodes.OutboundPayoutCapabilityRequired));
        }

        return Task.FromResult(MobileMoneyPayoutEligibilityResult.Eligible());
    }

    private static bool SameRoute(
        MobileMoneyPayoutCorridor configured,
        MobileMoneyPayoutCorridor requested) =>
        string.Equals(configured.SourceCountryCode, requested.SourceCountryCode, StringComparison.Ordinal) &&
        string.Equals(configured.SourceCurrency, requested.SourceCurrency, StringComparison.Ordinal) &&
        string.Equals(configured.DestinationCountryCode, requested.DestinationCountryCode, StringComparison.Ordinal) &&
        string.Equals(configured.DestinationCurrency, requested.DestinationCurrency, StringComparison.Ordinal);
}
