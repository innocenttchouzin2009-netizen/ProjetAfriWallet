namespace MobileMoney.Production.BeneficiaryResolution.Domain;

public sealed record BeneficiaryResolutionCandidate(
    string ResolutionId,
    string NormalizedPhoneNumber,
    string CountryCode,
    string? OperatorCode,
    string? AccountHolderName,
    BeneficiaryResolutionStatus Status,
    BeneficiaryResolutionSource Source,
    bool RequiresOperatorConfirmation,
    bool ManualEntryAllowed,
    DateTimeOffset ResolvedAtUtc);
