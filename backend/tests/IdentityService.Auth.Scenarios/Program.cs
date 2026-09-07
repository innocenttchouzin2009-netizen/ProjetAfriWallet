using System.IdentityModel.Tokens.Jwt;
using IdentityService.Api.Auth.Abstractions;
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

Console.WriteLine("IdentityService.Auth security primitive scenarios passed.");

static void Run(string name, Action scenario)
{
    scenario();
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
