using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Contracts;
using IdentityService.Api.Auth.Domain;
using IdentityService.Api.Auth.Lifecycle;

namespace IdentityService.Api.Auth.Application;

public sealed class AuthApplicationService(
    IAuthUserStore userStore,
    IAuthSessionStore sessionStore,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer accessTokenIssuer,
    AuthSessionLifecycleService sessionLifecycle,
    IClock clock,
    AuthApplicationOptions options)
{
    public async Task<AuthOperationResult<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateIdentifierAndPassword(request.Identifier, request.Password);
        if (validationError is not null)
        {
            return AuthOperationResult<RegisterResponse>.Failure(AuthErrorCode.ValidationError, validationError);
        }

        var normalizedIdentifier = NormalizeIdentifier(request.Identifier);
        var user = new AuthUser(
            Guid.NewGuid(),
            normalizedIdentifier,
            passwordHasher.Hash(request.Password),
            false,
            clock.UtcNow);

        if (!await userStore.TryAddAsync(user, cancellationToken))
        {
            return AuthOperationResult<RegisterResponse>.Failure(
                AuthErrorCode.IdentifierAlreadyExists,
                "An account with this identifier already exists.");
        }

        return AuthOperationResult<RegisterResponse>.Success(new RegisterResponse(user.Id, "PENDING"));
    }

    public async Task<AuthOperationResult<AuthSessionResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.DeviceId) ||
            string.IsNullOrWhiteSpace(request.Platform))
        {
            return AuthOperationResult<AuthSessionResponse>.Failure(
                AuthErrorCode.ValidationError,
                "Identifier, password, deviceId and platform are required.");
        }

        var user = await userStore.FindByNormalizedIdentifierAsync(
            NormalizeIdentifier(request.Identifier), cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return AuthOperationResult<AuthSessionResponse>.Failure(
                AuthErrorCode.InvalidCredentials,
                "Invalid credentials.");
        }

        if (user.IsDisabled)
        {
            return AuthOperationResult<AuthSessionResponse>.Failure(
                AuthErrorCode.UserDisabled,
                "The account is disabled.");
        }

        var grant = await sessionLifecycle.CreateAsync(user.Id, request.DeviceId.Trim(), cancellationToken);
        return AuthOperationResult<AuthSessionResponse>.Success(CreateSessionResponse(grant.Session, grant.RefreshToken));
    }

    public async Task<AuthOperationResult<AuthSessionResponse>> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthOperationResult<AuthSessionResponse>.Failure(
                AuthErrorCode.ValidationError,
                "Refresh token is required.");
        }

        var rotation = await sessionLifecycle.RotateRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (rotation.Status != RefreshRotationStatus.Succeeded ||
            rotation.Session is null ||
            rotation.RefreshToken is null)
        {
            return rotation.Status switch
            {
                RefreshRotationStatus.Expired => AuthOperationResult<AuthSessionResponse>.Failure(AuthErrorCode.RefreshExpired, "Refresh token expired."),
                RefreshRotationStatus.Reused => AuthOperationResult<AuthSessionResponse>.Failure(AuthErrorCode.RefreshReused, "Refresh token reuse detected."),
                RefreshRotationStatus.Revoked => AuthOperationResult<AuthSessionResponse>.Failure(AuthErrorCode.SessionRevoked, "Session revoked."),
                _ => AuthOperationResult<AuthSessionResponse>.Failure(AuthErrorCode.RefreshInvalid, "Invalid refresh token.")
            };
        }

        return AuthOperationResult<AuthSessionResponse>.Success(
            CreateSessionResponse(rotation.Session, rotation.RefreshToken));
    }

    public async Task<AuthOperationResult<bool>> LogoutAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            return AuthOperationResult<bool>.Failure(AuthErrorCode.ValidationError, "Session id is required.");
        }

        await sessionLifecycle.RevokeAsync(sessionId, "logout", cancellationToken);
        return AuthOperationResult<bool>.Success(true);
    }

    public async Task<AuthOperationResult<bool>> LogoutAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return AuthOperationResult<bool>.Failure(AuthErrorCode.ValidationError, "User id is required.");
        }

        await sessionLifecycle.RevokeAllForUserAsync(userId, "logout_all", cancellationToken);
        return AuthOperationResult<bool>.Success(true);
    }

    public async Task<AuthOperationResult<CurrentSessionResponse>> GetSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || sessionId == Guid.Empty)
        {
            return AuthOperationResult<CurrentSessionResponse>.Failure(
                AuthErrorCode.ValidationError,
                "User id and session id are required.");
        }

        var session = await sessionStore.GetByIdAsync(sessionId, cancellationToken);
        if (session is null || session.UserId != userId)
        {
            return AuthOperationResult<CurrentSessionResponse>.Failure(
                AuthErrorCode.TokenInvalid,
                "Session not found.");
        }

        if (session.Status == AuthSessionStatus.Revoked)
        {
            return AuthOperationResult<CurrentSessionResponse>.Failure(
                AuthErrorCode.SessionRevoked,
                "Session revoked.");
        }

        if (session.Status == AuthSessionStatus.Expired || clock.UtcNow >= session.ExpiresAtUtc)
        {
            return AuthOperationResult<CurrentSessionResponse>.Failure(
                AuthErrorCode.SessionExpired,
                "Session expired.");
        }

        return AuthOperationResult<CurrentSessionResponse>.Success(
            new CurrentSessionResponse(
                session.UserId,
                session.Id,
                session.DeviceId,
                session.CreatedAtUtc,
                session.ExpiresAtUtc,
                session.TokenVersion));
    }

    private AuthSessionResponse CreateSessionResponse(AuthSession session, string refreshToken)
    {
        var now = clock.UtcNow;
        var accessTokenExpiresAt = now.Add(options.AccessTokenLifetime);
        if (accessTokenExpiresAt > session.ExpiresAtUtc)
        {
            accessTokenExpiresAt = session.ExpiresAtUtc;
        }

        var accessToken = accessTokenIssuer.Issue(
            session.UserId,
            session.Id,
            session.TokenVersion,
            accessTokenExpiresAt);

        var expiresIn = Math.Max(0L, (long)(accessTokenExpiresAt - now).TotalSeconds);
        return new AuthSessionResponse(
            accessToken,
            refreshToken,
            "Bearer",
            expiresIn,
            session.Id,
            session.UserId);
    }

    private static string NormalizeIdentifier(string identifier) =>
        identifier.Trim().ToLowerInvariant();

    private static string? ValidateIdentifierAndPassword(string identifier, string password)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return "Identifier is required.";
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            return "Password must contain at least 12 characters.";
        }

        return null;
    }
}
