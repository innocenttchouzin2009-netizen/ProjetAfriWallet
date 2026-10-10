namespace MobileMoney.Production.Auth;

public sealed record AfWalSessionValidationResult(
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static AfWalSessionValidationResult Success() =>
        new(true, null, null);

    public static AfWalSessionValidationResult Failure(
        string errorCode,
        string errorMessage) =>
        new(false, errorCode, errorMessage);
}
