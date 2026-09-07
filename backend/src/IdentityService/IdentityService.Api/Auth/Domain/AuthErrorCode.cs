namespace IdentityService.Api.Auth.Domain;

public static class AuthErrorCode
{
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string IdentifierAlreadyExists = "AUTH_IDENTIFIER_ALREADY_EXISTS";
    public const string SessionExpired = "AUTH_SESSION_EXPIRED";
    public const string SessionRevoked = "AUTH_SESSION_REVOKED";
    public const string RefreshInvalid = "AUTH_REFRESH_INVALID";
    public const string RefreshExpired = "AUTH_REFRESH_EXPIRED";
    public const string RefreshReused = "AUTH_REFRESH_REUSED";
    public const string TokenInvalid = "AUTH_TOKEN_INVALID";
    public const string TokenExpired = "AUTH_TOKEN_EXPIRED";
    public const string UserDisabled = "AUTH_USER_DISABLED";
    public const string ValidationError = "AUTH_VALIDATION_ERROR";
}
