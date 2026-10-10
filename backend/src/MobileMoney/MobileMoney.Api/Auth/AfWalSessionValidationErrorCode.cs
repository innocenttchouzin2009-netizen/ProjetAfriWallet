namespace MobileMoney.Production.Auth;

public static class AfWalSessionValidationErrorCode
{
    public const string TokenInvalid = "AUTH_TOKEN_INVALID";
    public const string SessionExpired = "AUTH_SESSION_EXPIRED";
    public const string SessionRevoked = "AUTH_SESSION_REVOKED";
    public const string SessionUnavailable = "AUTH_SESSION_VALIDATION_UNAVAILABLE";
    public const string Forbidden = "AUTH_FORBIDDEN";
}
