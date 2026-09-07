namespace IdentityService.Api.Auth.Endpoints;

public static class AuthRoutes
{
    public const string Register = "/api/v1/auth/register";
    public const string Login = "/api/v1/auth/login";
    public const string Refresh = "/api/v1/auth/refresh";
    public const string Logout = "/api/v1/auth/logout";
    public const string LogoutAll = "/api/v1/auth/logout-all";
    public const string Session = "/api/v1/auth/session";
}
