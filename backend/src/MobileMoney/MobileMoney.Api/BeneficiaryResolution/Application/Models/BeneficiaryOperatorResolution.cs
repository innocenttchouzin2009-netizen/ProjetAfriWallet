using MobileMoney.Production.BeneficiaryResolution.Domain;

namespace MobileMoney.Production.BeneficiaryResolution.Application.Models;

public sealed record BeneficiaryOperatorResolution(
    string OperatorCode,
    BeneficiaryResolutionSource Source,
    bool RequiresConfirmation);
