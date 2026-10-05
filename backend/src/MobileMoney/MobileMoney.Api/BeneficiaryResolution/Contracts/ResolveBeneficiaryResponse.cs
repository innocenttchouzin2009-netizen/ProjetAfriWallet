namespace MobileMoney.Production.BeneficiaryResolution.Contracts;

public sealed record ResolveBeneficiaryResponse(
    string ResolutionId,
    string NormalizedPhoneNumber,
    string CountryCode,
    string? OperatorCode,
    string? AccountHolderName,
    string Status,
    string Source,
    bool RequiresOperatorConfirmation,
    bool ManualEntryAllowed);
