using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.P2P.Directory.Persistence;
using IdentityService.Api.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var ownerId = Guid.NewGuid();
var foreignOwnerId = Guid.NewGuid();
const string afWalId = "owner.current";

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseTestServer();
builder.Services
    .AddAuthentication("Test")
    .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddDbContext<RecipientDirectoryDbContext>(options => options.UseSqlite(connection));
builder.Services.AddScoped<ICurrentAfWalIdentityReader, EfCurrentAfWalIdentityReader>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapIdentityEndpoints();
await app.StartAsync();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecipientDirectoryDbContext>();
    await db.Database.EnsureCreatedAsync();
    db.AfWalIdentities.Add(new AfWalIdentityEntry
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        AfWalId = afWalId,
        IsActive = true
    });
    db.AfWalIdentities.Add(new AfWalIdentityEntry
    {
        Id = Guid.NewGuid(),
        OwnerId = foreignOwnerId,
        AfWalId = "foreign.inactive",
        IsActive = false
    });
    await db.SaveChangesAsync();
}

var anonymous = app.GetTestClient();
var ownerClient = CreateClient(app, ownerId);
var foreignClient = CreateClient(app, foreignOwnerId);

await RunAsync("current identity requires authentication", async () =>
{
    var response = await anonymous.GetAsync("/api/v1/identity/current");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("authenticated owner reads only own active AfWal identity", async () =>
{
    var response = await ownerClient.GetAsync("/api/v1/identity/current");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");

    var result = await response.Content.ReadFromJsonAsync<CurrentIdentityResponse>();
    Assert(result is not null, "Current identity response is required.");
    Assert(result!.UserId == ownerId, "UserId must come from the authenticated owner.");
    Assert(result.AfWalId == afWalId, "AfWalId mismatch.");
});

await RunAsync("authenticated foreign owner cannot read another owner's identity", async () =>
{
    var response = await foreignClient.GetAsync("/api/v1/identity/current");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("owner without active identity returns 404", async () =>
{
    var response = await CreateClient(app, Guid.NewGuid()).GetAsync("/api/v1/identity/current");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("inactive identity is not returned", async () =>
{
    var response = await foreignClient.GetAsync("/api/v1/identity/current");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected inactive identity to be hidden with 404, got {(int)response.StatusCode}.");
});

await RunAsync("only one active AfWal ID is allowed per owner", async () =>
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<RecipientDirectoryDbContext>();

    db.AfWalIdentities.Add(new AfWalIdentityEntry
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        AfWalId = "owner.second-active",
        IsActive = true
    });

    var conflict = false;
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        conflict = true;
    }
    finally
    {
        db.ChangeTracker.Clear();
    }

    Assert(conflict, "A second active AfWal ID for the same owner must be rejected.");
});

await RunAsync("inactive history remains allowed for the same owner", async () =>
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<RecipientDirectoryDbContext>();

    db.AfWalIdentities.Add(new AfWalIdentityEntry
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        AfWalId = "owner.previous",
        IsActive = false
    });

    await db.SaveChangesAsync();
    var inactiveCount = await db.AfWalIdentities.CountAsync(x => x.OwnerId == ownerId && !x.IsActive);
    Assert(inactiveCount == 1, "Inactive AfWal ID history must remain allowed.");
});

Console.WriteLine("AFW-BE-IDENTITY-CURRENT-1 authenticated current AfWal identity scenarios: PASS");

await app.DisposeAsync();

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
