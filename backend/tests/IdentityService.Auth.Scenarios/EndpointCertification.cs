using System.Runtime.CompilerServices;
using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Application;
using IdentityService.Api.Auth.Contracts;
using IdentityService.Api.Auth.Domain;
using IdentityService.Api.Auth.Endpoints;
using IdentityService.Api.Auth.Lifecycle;
using IdentityService.Api.Auth.Security;

internal static class EndpointCertification
{
    [ModuleInitializer]
    internal static void Initialize() => RunAsync().GetAwaiter().GetResult();

    private static async Task RunAsync()
    {
        CertifyRoutes();
        CertifyAccessTokenValidation();
        await CertifyApplicationFlowAsync();
        Console.WriteLine("IdentityService.Auth endpoint and API certification scenarios passed.");
    }

    private static void CertifyRoutes()
    {
        Assert(AuthRoutes.Register == "/api/v1/auth/register", "Register route contract changed.");
        Assert(AuthRoutes.Login == "/api/v1/auth/login", "Login route contract changed.");
        Assert(AuthRoutes.Refresh == "/api/v1/auth/refresh", "Refresh route contract changed.");
        Assert(AuthRoutes.Logout == "/api/v1/auth/logout", "Logout route contract changed.");
        Assert(AuthRoutes.LogoutAll == "/api/v1/auth/logout-all", "Logout-all route contract changed.");
        Assert(AuthRoutes.Session == "/api/v1/auth/session", "Session route contract changed.");
    }

    private static void CertifyAccessTokenValidation()
    {
        var options = new JwtAccessTokenOptions(
            "https://identity.afrikawallet.test",
            "afrikawallet-mobile",
            "0123456789abcdef0123456789abcdef");
        IClock clock = new CertificationClock(DateTimeOffset.UtcNow);
        var issuer = new JwtAccessTokenIssuer(options, clock);
        var validator = new JwtAccessTokenValidator(options);
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var token = issuer.Issue(userId, sessionId, 4, clock.UtcNow.AddMinutes(5));

        var principal = validator.Validate(token);
        Assert(principal is not null, "Valid access token must validate.");
        Assert(principal!.UserId == userId, "Validated user id mismatch.");
        Assert(principal.SessionId == sessionId, "Validated session id mismatch.");
        Assert(principal.TokenVersion == 4, "Validated token version mismatch.");
        Assert(validator.Validate(token + "tampered") is null, "Tampered access token must fail validation.");
    }

    private static async Task CertifyApplicationFlowAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var clock = new CertificationClock(now);
        var passwordHasher = new AspNetPasswordHasher();
        var refreshTokens = new CryptographicRefreshTokenService();
        var sessions = new InMemoryAuthSessionStore();
        var users = new InMemoryAuthUserStore();
        var jwtOptions = new JwtAccessTokenOptions(
            "https://identity.afrikawallet.test",
            "afrikawallet-mobile",
            "0123456789abcdef0123456789abcdef");
        var lifecycle = new AuthSessionLifecycleService(
            sessions,
            refreshTokens,
            clock,
            AuthSessionOptions.Default);
        var auth = new AuthApplicationService(
            users,
            sessions,
            passwordHasher,
            new JwtAccessTokenIssuer(jwtOptions, clock),
            lifecycle,
            clock,
            AuthApplicationOptions.Default);

        var register = await auth.RegisterAsync(new RegisterRequest("User@Example.com", "Correct-Horse-Battery-Staple-42"));
        Assert(register.Succeeded && register.Value is not null, "Registration contract must succeed.");

        var duplicate = await auth.RegisterAsync(new RegisterRequest(" user@example.com ", "Correct-Horse-Battery-Staple-42"));
        Assert(!duplicate.Succeeded && duplicate.ErrorCode == AuthErrorCode.IdentifierAlreadyExists, "Duplicate identifiers must be rejected after normalization.");

        var invalidLogin = await auth.LoginAsync(new LoginRequest("user@example.com", "wrong-password", "device-1", "android", null));
        Assert(!invalidLogin.Succeeded && invalidLogin.ErrorCode == AuthErrorCode.InvalidCredentials, "Invalid credentials must be rejected safely.");

        var login = await auth.LoginAsync(new LoginRequest("user@example.com", "Correct-Horse-Battery-Staple-42", "device-1", "android", "Test Device"));
        Assert(login.Succeeded && login.Value is not null, "Login contract must issue a session.");

        var session = await auth.GetSessionAsync(login.Value!.UserId, login.Value.SessionId);
        Assert(session.Succeeded && session.Value is not null, "Current session contract must resolve active session.");

        var refresh = await auth.RefreshAsync(new RefreshRequest(login.Value.RefreshToken));
        Assert(refresh.Succeeded && refresh.Value is not null, "Refresh contract must rotate token.");
        Assert(refresh.Value!.RefreshToken != login.Value.RefreshToken, "Refresh contract must return a rotated token.");

        var replay = await auth.RefreshAsync(new RefreshRequest(login.Value.RefreshToken));
        Assert(!replay.Succeeded && replay.ErrorCode == AuthErrorCode.RefreshReused, "Consumed refresh token replay must be detected.");

        var revokedSession = await auth.GetSessionAsync(login.Value.UserId, login.Value.SessionId);
        Assert(!revokedSession.Succeeded && revokedSession.ErrorCode == AuthErrorCode.SessionRevoked, "Reuse detection must revoke the compromised session.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class CertificationClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
