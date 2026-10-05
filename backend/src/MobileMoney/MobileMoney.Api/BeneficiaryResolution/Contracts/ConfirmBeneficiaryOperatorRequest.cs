namespace MobileMoney.Production.BeneficiaryResolution.Contracts;

public sealed record ConfirmBeneficiaryOperatorRequest(
    string ResolutionId,
    string OperatorCode);
