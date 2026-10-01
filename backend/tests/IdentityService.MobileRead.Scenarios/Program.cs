using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Application;
using IdentityService.Api.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var user = new AuthUser(
    Guid.NewGuid(),
    "profile.user@example.com",
    "not-exposed",
    false,
    new DateTimeOffset(2026, 9, 29, 18, 0, 0, TimeSpan.Zero));

await using var app = await BuildAppAsync(user);
var anonymous = app.GetTestClient();

await RunAsync("current profile requires authentication", async () =>
{
    var response = await anonymous.GetAsync(IdentityEndpoints.CurrentProfileRoute);
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("current profile returns authenticated auth-user projection", async () =>
{
    var client = CreateClient(app, user.Id);
    var response = await client.GetAsync(IdentityEndpoints.CurrentProfileRoute);

    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var profile = await response.Content.ReadFromJsonAsync<CurrentProfileReadModel>();

    Assert(profile is not null, "Current profile response is required.");
    Assert(profile!.UserId == user.Id, "Profile user id must come from authenticated sub claim.");
    Assert(profile.Identifier == user.NormalizedIdentifier, "Persisted normalized identifier must be projected.");
    Assert(profile.CreatedAtUtc == user.CreatedAtUtc, "Created timestamp must be projected.");
});

await RunAsync("current profile does not expose another authenticated user", async () =>
{
    var client = CreateClient(app, Guid.NewGuid());
    var response = await client.GetAsync(IdentityEndpoints.CurrentProfileRoute);

    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

Console.WriteLine("Identity current-profile mobile read scenarios passed.");

static async Task<WebApplication> BuildAppAsync(AuthUser user)
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();

    builder.Services
        .AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
    builder.Services.AddAuthorization();
    builder.Services.AddSingleton<IAuthUserStore>(new FixedAuthUserStore(user));
    builder.Services.AddScoped<CurrentProfileReadService>();

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapIdentityEndpoints();
    await app.StartAsync();
    return app;
}

static HttpClient CreateClient(WebApplication app, Guid userId)
{
    var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
    return client;
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

sealed class FixedAuthUserStore(AuthUser user) : IAuthUserStore
{
    public Task<AuthUser?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<AuthUser?>(user.Id == userId ? user : null);
    }

    public Task<AuthUser?> FindByNormalizedIdentifierAsync(
        string normalizedIdentifier,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<AuthUser?>(
            string.Equals(user.NormalizedIdentifier, normalizedIdentifier, StringComparison.Ordinal)
                ? user
                : null);
    }

    public Task<bool> TryAddAsync(
        AuthUser candidate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }
}

sealed class HeaderTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var raw) ||
            !Guid.TryParse(raw.ToString(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            [new Claim("sub", userId.ToString())],
            Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(
            AuthenticateResult.Success(
                new AuthenticationTicket(principal, Scheme.Name)));
    }
}
