using MobileMoney.Production.BeneficiaryResolution.Application.Abstractions;
using MobileMoney.Production.BeneficiaryResolution.Application.Models;

internal sealed class FakeCountryResolver(
    Func<string, string?, BeneficiaryCountryResolution?> resolve)
    : IBeneficiaryCountryResolver
{
    public int CallCount { get; private set; }

    public Task<BeneficiaryCountryResolution?> ResolveAsync(
        string normalizedPhoneNumber,
        string? countryCodeHint,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        return Task.FromResult(resolve(normalizedPhoneNumber, countryCodeHint));
    }
}

internal sealed class FakeOperatorResolver(
    Func<string, string, string?, BeneficiaryOperatorResolution?> resolve)
    : IBeneficiaryOperatorResolver
{
    public int CallCount { get; private set; }

    public Task<BeneficiaryOperatorResolution?> ResolveAsync(
        string normalizedPhoneNumber,
        string countryCode,
        string? operatorCodeHint,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        return Task.FromResult(
            resolve(normalizedPhoneNumber, countryCode, operatorCodeHint));
    }
}

internal sealed class FakeAccountHolderNameResolver(
    Func<string, string, string, BeneficiaryAccountHolderNameResolution?> resolve)
    : IBeneficiaryAccountHolderNameResolver
{
    public int CallCount { get; private set; }

    public Task<BeneficiaryAccountHolderNameResolution?> ResolveAsync(
        string normalizedPhoneNumber,
        string countryCode,
        string operatorCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        return Task.FromResult(
            resolve(normalizedPhoneNumber, countryCode, operatorCode));
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
