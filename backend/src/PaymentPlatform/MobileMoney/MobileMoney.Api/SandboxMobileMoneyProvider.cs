using System.Collections.Concurrent;
using AfriWallet.PaymentPlatform.MobileMoney.Application;
using AfriWallet.PaymentPlatform.MobileMoney.Domain;

namespace AfriWallet.PaymentPlatform.MobileMoney.Api;

public sealed class SandboxMobileMoneyProvider : IMobileMoneyProvider
{
    private readonly ConcurrentDictionary<string, MobileMoneyPaymentStatus> _transactions = new();

    public MobileMoneyProvider Definition { get; }

    public SandboxMobileMoneyProvider(
        string code,
        string name,
        IEnumerable<string> countries,
        IEnumerable<string> currencies)
    {
        Definition = new MobileMoneyProvider(
            code,
            name,
            countries
                .Select(country => country.ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase),
            currencies
                .Select(currency => currency.ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    public Task<ProviderPaymentResult> InitiateAsync(
        ProviderPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var reference = $"{Definition.Code}-{Guid.NewGuid():N}".ToUpperInvariant();
        _transactions[reference] = MobileMoneyPaymentStatus.Processing;

        return Task.FromResult(new ProviderPaymentResult(
            reference,
            MobileMoneyPaymentStatus.Processing));
    }

    public Task<ProviderStatusResult> GetStatusAsync(
        string providerReference,
        CancellationToken cancellationToken = default)
    {
        if (!_transactions.TryGetValue(providerReference, out var status))
            status = MobileMoneyPaymentStatus.Failed;

        return Task.FromResult(new ProviderStatusResult(providerReference, status));
    }

    public Task<MobileMoneyPaymentStatus> ProcessCallbackAsync(
        MobileMoneyCallback callback,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(
                callback.ProviderCode,
                Definition.Code,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new MobileMoneyException(
                "provider_mismatch",
                $"Callback provider '{callback.ProviderCode}' does not match '{Definition.Code}'.");
        }

        var status = callback.ExternalStatus.ToUpperInvariant() switch
        {
            "SUCCESS" or "SUCCESSFUL" => MobileMoneyPaymentStatus.Succeeded,
            "FAILED" => MobileMoneyPaymentStatus.Failed,
            "CANCELLED" => MobileMoneyPaymentStatus.Cancelled,
            "EXPIRED" => MobileMoneyPaymentStatus.Expired,
            _ => MobileMoneyPaymentStatus.Processing
        };

        _transactions[callback.ProviderReference] = status;
        return Task.FromResult(status);
    }
}