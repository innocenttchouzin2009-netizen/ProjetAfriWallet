using AfriWallet.Banking.Application.Contracts;
using AfriWallet.Banking.Domain.Entities;

namespace AfriWallet.Banking.Infrastructure;

public sealed class RegistryRepository : IBankProviderRepository
{
    private readonly List<BankProvider> _providers;

    public RegistryRepository()
    {
        _providers = SeedData.CreateSeedProviders();
    }

    public Task<IReadOnlyList<BankProvider>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BankProvider>>(_providers.AsReadOnly());

    public Task<BankProvider?> GetByIdAsync(string providerId, CancellationToken cancellationToken = default)
        => Task.FromResult(_providers.FirstOrDefault(p => p.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<BankProvider>> SearchAsync(string? country, string? currency, string? scheme, string? environment, CancellationToken cancellationToken = default)
    {
        var filtered = _providers.Where(p =>
            (string.IsNullOrWhiteSpace(country) || p.CountryCode.Equals(country, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(currency) || SupportsCurrency(p, currency)) &&
            (string.IsNullOrWhiteSpace(scheme) || MatchesScheme(p, scheme)) &&
            (string.IsNullOrWhiteSpace(environment) || p.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase))
        );

        return Task.FromResult<IReadOnlyList<BankProvider>>(filtered.ToList().AsReadOnly());
    }

    public Task<BankProvider> CreateAsync(BankProvider provider, CancellationToken cancellationToken = default)
    {
        var normalized = new BankProvider
        {
            ProviderId = provider.ProviderId,
            ProviderCode = provider.ProviderCode,
            DisplayName = provider.DisplayName,
            LegalName = provider.LegalName,
            CountryCode = provider.CountryCode,
            CurrencyCode = provider.CurrencyCode,
            SupportedCurrencies = provider.SupportedCurrencies,
            SwiftCode = provider.SwiftCode,
            Bic = provider.Bic,
            NationalClearingCode = provider.NationalClearingCode,
            TransferSchemes = provider.TransferSchemes,
            SupportsSepa = provider.SupportsSepa,
            SupportsSwift = provider.SupportsSwift,
            SupportsInstantPayments = provider.SupportsInstantPayments,
            SupportsDomesticTransfers = provider.SupportsDomesticTransfers,
            SettlementWindow = provider.SettlementWindow,
            CutoffTime = provider.CutoffTime,
            EstimatedDelivery = provider.EstimatedDelivery,
            EstimatedDeliveryDays = provider.EstimatedDeliveryDays,
            MinimumAmountMinor = provider.MinimumAmountMinor,
            MaximumAmountMinor = provider.MaximumAmountMinor,
            FixedFeeMinor = provider.FixedFeeMinor,
            PercentageFee = provider.PercentageFee,
            Environment = provider.Environment,
            Status = provider.Status,
            Priority = provider.Priority,
            MaintenanceMode = provider.MaintenanceMode,
            Capabilities = provider.Capabilities,
            CreatedUtc = provider.CreatedUtc,
            UpdatedUtc = provider.UpdatedUtc,
            CreatedAt = provider.CreatedAt,
            UpdatedAt = provider.UpdatedAt,
            Version = provider.Version
        };
        _providers.Add(normalized);
        return Task.FromResult(normalized);
    }

    public Task<BankProvider?> UpdateAsync(BankProvider provider, CancellationToken cancellationToken = default)
    {
        var existing = _providers.FirstOrDefault(p => p.ProviderId.Equals(provider.ProviderId, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            return Task.FromResult<BankProvider?>(null);
        }

        var index = _providers.IndexOf(existing);
        _providers[index] = provider;
        return Task.FromResult<BankProvider?>(provider);
    }

    private static bool MatchesScheme(BankProvider provider, string scheme)
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
}
