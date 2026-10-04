using MobileMoney.Production.BeneficiaryResolution.Domain;

namespace MobileMoney.Production.BeneficiaryResolution.Application.Models;

public sealed record BeneficiaryAccountHolderNameResolution(
    string AccountHolderName,
    BeneficiaryResolutionSource Source);
