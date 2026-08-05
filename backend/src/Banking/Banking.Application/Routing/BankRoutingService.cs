using System.Text.RegularExpressions;
using AfriWallet.Banking.Application.Contracts;
using AfriWallet.Banking.Domain.Entities;
using AfriWallet.Banking.Domain.Enums;
using AfriWallet.Banking.Domain.ValueObjects;

namespace AfriWallet.Banking.Application.Routing;

public sealed class BankRoutingService
{
    private static readonly Regex BicRegex = new("^[A-Z]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IBankProviderRepository _repository;

    public BankRoutingService(IBankProviderRepository repository)
    {
        _repository = repository;
    }

    public Task<(RoutingDecision Decision, BankProvider? Provider)> RouteAsync(RoutingKey routingKey, CancellationToken cancellationToken = default)
        => RouteAsync(routingKey, null, cancellationToken);

    public async Task<(RoutingDecision Decision, BankProvider? Provider)> RouteAsync(RoutingKey routingKey, decimal? amountMinor, CancellationToken cancellationToken = default)
    {
        var candidates = await _repository.SearchAsync(routingKey.Country, null, routingKey.Scheme, null, cancellationToken);
        var provider = candidates
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault();

        if (provider is null)
        {
            return (RoutingDecision.NotMatched, null);
        }

        if (provider.MaintenanceMode)
        {
            return (RoutingDecision.Unsupported, provider);
        }

        if (!provider.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            return (RoutingDecision.Inactive, provider);
        }

        if (!string.Equals(provider.Environment, routingKey.Environment, StringComparison.OrdinalIgnoreCase))
        {
            return (RoutingDecision.EnvironmentMismatch, provider);
        }

        if (!IsSchemeCompatible(provider, routingKey.Scheme))
        {
            return (RoutingDecision.Unsupported, provider);
        }

        if (!SupportsCurrency(provider, routingKey.Currency))
        {
            return (RoutingDecision.Unsupported, provider);
        }

        if (!IsValidBic(provider))
        {
            return (RoutingDecision.Unsupported, provider);
        }

        if (amountMinor.HasValue && !IsAmountWithinLimits(provider, amountMinor.Value))
        {
            return (RoutingDecision.Unsupported, provider);
        }

        if (!string.Equals(provider.CountryCode, routingKey.Country, StringComparison.OrdinalIgnoreCase))
        {
            return (RoutingDecision.NotMatched, provider);
        }

        return (RoutingDecision.Matched, provider);
    }

    private static bool IsSchemeCompatible(BankProvider provider, string scheme)
    {
        return scheme.ToLowerInvariant() switch
        {
            "sepa" => provider.SupportsSepa || provider.TransferSchemes.Contains("SEPA", StringComparer.OrdinalIgnoreCase),
            "swift" => provider.SupportsSwift || provider.TransferSchemes.Contains("SWIFT", StringComparer.OrdinalIgnoreCase),
            "domestic" => provider.SupportsDomesticTransfers || provider.TransferSchemes.Contains("DOMESTIC", StringComparer.OrdinalIgnoreCase),
            "instant" => provider.SupportsInstantPayments || provider.TransferSchemes.Contains("INSTANT", StringComparer.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool SupportsCurrency(BankProvider provider, string currency)
    {
        if (provider.SupportedCurrencies.Count > 0)
        {
            return provider.SupportedCurrencies.Contains(currency, StringComparer.OrdinalIgnoreCase);
        }

        return provider.CurrencyCode.Equals(currency, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidBic(BankProvider provider)
    {
        if (provider.SupportsSwift || provider.TransferSchemes.Contains("SWIFT", StringComparer.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(provider.Bic) ? false : BicRegex.IsMatch(provider.Bic.ToUpperInvariant());
        }

        if (provider.SupportsSepa || provider.TransferSchemes.Contains("SEPA", StringComparer.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(provider.Bic) ? false : BicRegex.IsMatch(provider.Bic.ToUpperInvariant());
        }

        return true;
    }

    private static bool IsAmountWithinLimits(BankProvider provider, decimal amountMinor)
    {
        return amountMinor >= provider.MinimumAmountMinor && amountMinor <= provider.MaximumAmountMinor;
    }
}
