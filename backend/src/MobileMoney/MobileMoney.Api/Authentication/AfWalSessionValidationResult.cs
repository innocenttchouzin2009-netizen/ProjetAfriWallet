namespace MobileMoney.Production.Authentication;

public sealed record AfWalSessionValidationResult(
    AfWalSessionValidationStatus Status,
    AfWalValidatedSession? Session = null)
{
    public bool Succeeded =>
        Status == AfWalSessionValidationStatus.Valid &&
        Session is not null;

    public static AfWalSessionValidationResult Valid(
        AfWalValidatedSession session) =>
        new(AfWalSessionValidationStatus.Valid, session);

    public static AfWalSessionValidationResult Failed(
        AfWalSessionValidationStatus status) =>
        new(status);
}
