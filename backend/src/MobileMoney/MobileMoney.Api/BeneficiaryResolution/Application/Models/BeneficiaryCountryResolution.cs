using MobileMoney.Production.BeneficiaryResolution.Domain;

namespace MobileMoney.Production.BeneficiaryResolution.Application.Models;

public sealed record BeneficiaryCountryResolution(
    string CountryCode,
    BeneficiaryResolutionSource Source);
