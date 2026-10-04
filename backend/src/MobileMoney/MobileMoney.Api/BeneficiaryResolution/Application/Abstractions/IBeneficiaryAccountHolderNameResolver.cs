using MobileMoney.Production.BeneficiaryResolution.Application.Models;

namespace MobileMoney.Production.BeneficiaryResolution.Application.Abstractions;

public interface IBeneficiaryAccountHolderNameResolver
{
    Task<BeneficiaryAccountHolderNameResolution?> ResolveAsync(
        string normalizedPhoneNumber,
        string countryCode,
        string operatorCode,
        CancellationToken cancellationToken = default);
}
