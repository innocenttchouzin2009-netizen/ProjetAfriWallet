using MobileMoney.Production.BeneficiaryResolution.Application.Models;

namespace MobileMoney.Production.BeneficiaryResolution.Application.Abstractions;

public interface IBeneficiaryOperatorResolver
{
    Task<BeneficiaryOperatorResolution?> ResolveAsync(
        string normalizedPhoneNumber,
        string countryCode,
        string? operatorCodeHint,
        CancellationToken cancellationToken = default);
}
