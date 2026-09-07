using System.IdentityModel.Tokens.Jwt;
using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Domain;
using IdentityService.Api.Auth.Lifecycle;
using IdentityService.Api.Auth.Security;

Run("password hash verifies correct password and rejects wrong password", () =>
{
    var hasher = new AspNetPasswordHasher();
    const string password = "Correct-Horse-Battery-Staple-42";
    var hash = hasher.Hash(password);

    Assert(hash != password, "Password hash must not equal plaintext.");
    Assert(hasher.Verify(password, hash), "Correct password must verify.");
    Assert(!hasher.Verify("wrong-password", hash), "Wrong password must be rejected.");
});

Run("refresh tokens are random, url-safe and hashes are deterministic", () =>
{
    var service = new CryptographicRefreshTokenService();
    var first = service.Generate();
    var second = service.Generate();

    Assert(first != second, "Two generated refresh tokens must differ.");
    Assert(!first.Contains('+') && !first.Contains('/') && !first.Contains('='), "Refresh token must be base64url-safe.");
    Assert(service.Hash(first) == service.Hash(first), "Refresh token hash must be deterministic.");
    Assert(service.Hash(first) != first, "Stored refresh hash must not equal the raw refresh token.");
});

Run("system clock returns UTC time", () =>
{
    IClock clock = new SystemClock();
    var delta = (DateTimeOffset.UtcNow - clock.UtcNow).Duration();
    Assert(delta < TimeSpan.FromSeconds(2), "System clock must track UTC time.");
});

Run("JWT issuer emits required claims and metadata", () =>
{
    var now = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);
    var clock = new FixedClock(now);
    var options = new JwtAccessTokenOptions(
        "https://identity.afrikawallet.test",
        "afrikawallet-mobile",
        "0123456789abcdef0123456789abcdef");
    var issuer = new JwtAccessTokenIssuer(options, clock);
    var userId = Guid.NewGuid();
    var sessionId = Guid.NewGuid();
    var token = issuer.Issue(userId, sessionId, 7, now.AddMinutes(15));

    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
    Assert(jwt.Issuer == options.Issuer, "JWT issuer mismatch.");
    Assert(jwt.Audiences.Single() == options.Audience, "JWT audience mismatch.");
    Assert(jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value == userId.ToString(), "JWT sub mismatch.");
    Assert(jwt.Claims.Single(c => c.Type == "sid").Value == sessionId.ToString(), "JWT sid mismatch.");
    Assert(jwt.Claims.Single(c => c.Type == "ver").Value == "7", "JWT version mismatch.");
    Assert(jwt.ValidTo == now.AddMinutes(15).UtcDateTime, "JWT expiration mismatch.");
});

Run("JWT issuer rejects expired issuance requests", () =>
{
    var now = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);
    var issuer = new JwtAccessTokenIssuer(
        new JwtAccessTokenOptions("issuer", "audience", "0123456789abcdef0123456789abcdef"),
        new FixedClock(now));

    try
    {
        issuer.Issue(Guid.NewGuid(), Guid.NewGuid(), 1, now);
        throw new InvalidOperationException("Expected expired issuance to fail.");
    }
    catch (ArgumentOutOfRangeException)
    {
    }
});

await RunAsync("session creation stores only refresh token hash", async () =>
{
    var clock = new MutableClock(new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero));
    var refreshTokens = new CryptographicRefreshTokenService();
    var store = new InMemoryAuthSessionStore();
    var service = new AuthSessionLifecycleService(store, refreshTokens, clock, new AuthSessionOptions(TimeSpan.FromDays(30)));

    var grant = await service.CreateAsync(Guid.NewGuid(), "device-1");
    var persisted = await store.GetByIdAsync(grant.Session.Id);

    Assert(persisted is not null, "Created session must be persisted.");
    Assert(persisted!.RefreshTokenHash != grant.RefreshToken, "Raw refresh token must never be stored.");
    Assert(persisted.RefreshTokenHash == refreshTokens.Hash(grant.RefreshToken), "Persisted refresh hash mismatch.");
    Assert(persisted.Status == AuthSessionStatus.Active, "New session must be active.");
});

await RunAsync("refresh rotation is atomic and advances token version", async () =>
{
    var clock = new MutableClock(new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero));
    var refreshTokens = new CryptographicRefreshTokenService();
    var store = new InMemoryAuthSessionStore();
    var service = new AuthSessionLifecycleService(store, refreshTokens, clock, AuthSessionOptions.Default);
    var grant = await service.CreateAsync(Guid.NewGuid(), "device-2");

    clock.Advance(TimeSpan.FromMinutes(1));
    var rotated = await service.RotateRefreshTokenAsync(grant.RefreshToken);

    Assert(rotated.Status == RefreshRotationStatus.Succeeded, "First rotation must succeed.");
    Assert(rotated.RefreshToken is not null && rotated.RefreshToken != grant.RefreshToken, "Rotation must issue a new token.");
    Assert(rotated.Session!.TokenVersion == grant.Session.TokenVersion + 1, "Rotation must advance token version.");
    Assert(rotated.Session.ExpiresAtUtc == grant.Session.ExpiresAtUtc, "Rotation must not extend absolute session expiry.");
});

await RunAsync("reuse of a consumed refresh token revokes the session", async () =>
{
    var clock = new MutableClock(new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero));
    var refreshTokens = new CryptographicRefreshTokenService();
    var store = new InMemoryAuthSessionStore();
    var service = new AuthSessionLifecycleService(store, refreshTokens, clock, AuthSessionOptions.Default);
    var grant = await service.CreateAsync(Guid.NewGuid(), "device-3");

    var firstRotation = await service.RotateRefreshTokenAsync(grant.RefreshToken);
    Assert(firstRotation.Status == RefreshRotationStatus.Succeeded, "Initial rotation must succeed.");

    var reuse = await service.RotateRefreshTokenAsync(grant.RefreshToken);
    Assert(reuse.Status == RefreshRotationStatus.Reused, "Consumed refresh token must be detected as reuse.");

    var persisted = await store.GetByIdAsync(grant.Session.Id);
    Assert(persisted!.Status == AuthSessionStatus.Revoked, "Reuse must revoke the session.");
    Assert(persisted.RevocationReason == "refresh_token_reuse", "Reuse revocation reason mismatch.");

    var replacementAttempt = await service.RotateRefreshTokenAsync(firstRotation.RefreshToken!);
    Assert(replacementAttempt.Status == RefreshRotationStatus.Revoked, "Replacement token must fail after family revocation.");
});

await RunAsync("expired sessions cannot rotate refresh tokens", async () =>
{
    var clock = new MutableClock(new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero));
    var refreshTokens = new CryptographicRefreshTokenService();
    var store = new InMemoryAuthSessionStore();
    var service = new AuthSessionLifecycleService(store, refreshTokens, clock, new AuthSessionOptions(TimeSpan.FromMinutes(5)));
    var grant = await service.CreateAsync(Guid.NewGuid(), "device-4");

    clock.Advance(TimeSpan.FromMinutes(5));
    var result = await service.RotateRefreshTokenAsync(grant.RefreshToken);

    Assert(result.Status == RefreshRotationStatus.Expired, "Rotation at the expiry instant must be rejected.");
    Assert(result.Session!.Status == AuthSessionStatus.Expired, "Expired session status must be persisted.");
});

await RunAsync("explicit revoke and revoke-all terminate sessions", async () =>
{
    var clock = new MutableClock(new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero));
    var refreshTokens = new CryptographicRefreshTokenService();
    var store = new InMemoryAuthSessionStore();
    var service = new AuthSessionLifecycleService(store, refreshTokens, clock, AuthSessionOptions.Default);
    var userId = Guid.NewGuid();
    var first = await service.CreateAsync(userId, "device-a");
    var second = await service.CreateAsync(userId, "device-b");

    await service.RevokeAsync(first.Session.Id, "logout");
    Assert((await store.GetByIdAsync(first.Session.Id))!.Status == AuthSessionStatus.Revoked, "Single revoke must revoke target session.");

    await service.RevokeAllForUserAsync(userId, "logout_all");
    Assert((await store.GetByIdAsync(second.Session.Id))!.Status == AuthSessionStatus.Revoked, "Revoke-all must revoke remaining user sessions.");
});

await RunAsync("concurrent refresh attempts allow one rotation and detect reuse", async () =>
{
    var clock = new MutableClock(new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero));
    var refreshTokens = new CryptographicRefreshTokenService();
    var store = new InMemoryAuthSessionStore();
    var service = new AuthSessionLifecycleService(store, refreshTokens, clock, AuthSessionOptions.Default);
    var grant = await service.CreateAsync(Guid.NewGuid(), "device-concurrent");

    var attempts = Enumerable.Range(0, 12)
        .Select(_ => service.RotateRefreshTokenAsync(grant.RefreshToken))
        .ToArray();
    var results = await Task.WhenAll(attempts);

    Assert(results.Count(result => result.Status == RefreshRotationStatus.Succeeded) == 1, "Exactly one concurrent rotation must succeed.");
    Assert(results.Any(result => result.Status == RefreshRotationStatus.Reused), "Concurrent replay must be detected as reuse.");

    var persisted = await store.GetByIdAsync(grant.Session.Id);
    Assert(persisted!.Status == AuthSessionStatus.Revoked, "Concurrent reuse detection must revoke the compromised session.");
});

Console.WriteLine("IdentityService.Auth security and session lifecycle scenarios passed.");

static void Run(string name, Action scenario)
{
    scenario();
    Console.WriteLine($"PASS: {name}");
}

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

sealed class MutableClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
}
