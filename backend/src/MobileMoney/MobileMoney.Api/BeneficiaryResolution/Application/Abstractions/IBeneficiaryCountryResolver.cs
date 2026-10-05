using MobileMoney.Production.BeneficiaryResolution.Application.Models;

namespace MobileMoney.Production.BeneficiaryResolution.Application.Abstractions;

public interface IBeneficiaryCountryResolver
{
    Task<BeneficiaryCountryResolution?> ResolveAsync(
        string normalizedPhoneNumber,
        string? countryCodeHint,
        CancellationToken cancellationToken = default);
}
