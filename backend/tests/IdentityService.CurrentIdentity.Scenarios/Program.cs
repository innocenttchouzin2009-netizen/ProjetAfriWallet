using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.P2P.Directory.Persistence;
using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Application;
using IdentityService.Api.Auth.Domain;
using IdentityService.Api.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var ownerId = Guid.NewGuid();
var foreignOwnerId = Guid.NewGuid();
var createdAt = new DateTimeOffset(2026, 10, 1, 8, 30, 0, TimeSpan.Zero);
var owner = new AuthUser(
    ownerId,
    "current.identity@example.com",
    "not-exposed",
    false,
    createdAt);

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

await using var app = await BuildAppAsync(owner, connection);

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecipientDirectoryDbContext>();
    await db.Database.EnsureCreatedAsync();
    db.AfWalIdentities.AddRange(
        new AfWalIdentityEntry
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            AfWalId = "current.user",
            IsActive = true
        },
        new AfWalIdentityEntry
        {
            Id = Guid.NewGuid(),
            OwnerId = foreignOwnerId,
            AfWalId = "foreign.inactive",
            IsActive = false
        });
    await db.SaveChangesAsync();
}

var anonymous = app.GetTestClient();

await RunAsync("current identity requires authentication", async () =>
{
    var response = await anonymous.GetAsync(IdentityEndpoints.CurrentIdentityRoute);
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("current identity composes profile and active AfWal ID", async () =>
{
    var client = CreateClient(app, ownerId);
    var response = await client.GetAsync(IdentityEndpoints.CurrentIdentityRoute);

    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var current = await response.Content.ReadFromJsonAsync<CurrentIdentityReadModel>();

    Assert(current is not null, "Current identity response is required.");
    Assert(current!.UserId == ownerId, "User id must come from authenticated subject.");
    Assert(current.Identifier == owner.NormalizedIdentifier, "Profile identifier must be preserved.");
    Assert(current.CreatedAtUtc == createdAt, "Profile creation timestamp must be preserved.");
    Assert(current.AfWalId == "current.user", "Active AfWal ID must be composed into the response.");
});

await RunAsync("current identity does not expose another user", async () =>
{
    var client = CreateClient(app, foreignOwnerId);
    var response = await client.GetAsync(IdentityEndpoints.CurrentIdentityRoute);

    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("current identity requires an active AfWal ID", async () =>
{
    var client = CreateClient(app, ownerId);

    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RecipientDirectoryDbContext>();
        var entry = await db.AfWalIdentities.SingleAsync(x => x.OwnerId == ownerId);
        entry.IsActive = false;
        await db.SaveChangesAsync();
    }

    var response = await client.GetAsync(IdentityEndpoints.CurrentIdentityRoute);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

Console.WriteLine("AFW-BE-IDENTITY-CURRENT-1 scenarios passed.");

static async Task<WebApplication> BuildAppAsync(AuthUser user, SqliteConnection connection)
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();

    builder.Services
        .AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
    builder.Services.AddAuthorization();

    builder.Services.AddSingleton<IAuthUserStore>(new FixedAuthUserStore(user));
    builder.Services.AddScoped<CurrentProfileReadService>();
    builder.Services.AddScoped<CurrentIdentityReadService>();
    builder.Services.AddDbContext<RecipientDirectoryDbContext>(options => options.UseSqlite(connection));
    builder.Services.AddScoped<ICurrentAfWalIdentityReader, EfCurrentAfWalIdentityReader>();

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

        return Task.FromResult(
            AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
