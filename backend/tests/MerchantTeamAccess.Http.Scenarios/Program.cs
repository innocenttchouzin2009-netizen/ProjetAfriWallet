using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Merchants.Registry.Application.Abstractions;
using AfriWallet.Merchants.Registry.Domain.Merchants;
using AfriWallet.Merchants.Registry.Domain.Profiles;
using AfriWallet.Merchants.Registry.Infrastructure;
using AfriWallet.Merchants.TeamAccess.Application;
using AfriWallet.Merchants.TeamAccess.Domain;
using AfriWallet.Merchants.TeamAccess.Infrastructure;
using AfriWallet.P2P.Directory.Persistence;
using AfriWallet.P2P.Infrastructure;
using IdentityService.Api.MerchantTeamAccess;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await MerchantTeamFixture.CreateAsync();

var anonymous = fixture.App.GetTestClient();
var owner = fixture.CreateClient(fixture.OwnerUserId);
var admin = fixture.CreateClient(fixture.AdminUserId);
var readOnly = fixture.CreateClient(fixture.ReadOnlyUserId);
var foreign = fixture.CreateClient(fixture.ForeignUserId);
var noIdentity = fixture.CreateClient(fixture.NoIdentityUserId);

await RunAsync("authentication required", async () =>
{
    var response = await anonymous.GetAsync($"/api/v1/merchants/{fixture.MerchantId}/team");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, "Expected 401.");
});

await RunAsync("owner bootstraps from authenticated personal AfWal ID", async () =>
{
    var response = await owner.GetAsync($"/api/v1/merchants/{fixture.MerchantId}/team/me");
    Assert(response.StatusCode == HttpStatusCode.OK, "Expected 200.");
    var body = await response.Content.ReadFromJsonAsync<MerchantTeamMemberResponse>();
    Assert(body?.Role == "Owner", "Owner role expected.");
});

await RunAsync("owner adds admin by active AfWal ID", async () =>
{
    var response = await owner.PostAsJsonAsync(
        $"/api/v1/merchants/{fixture.MerchantId}/team",
        new AddMerchantTeamMemberRequest(fixture.AdminAwid, "Admin"));
    Assert(response.StatusCode == HttpStatusCode.Created, "Expected 201.");
});

await RunAsync("admin can add readonly member", async () =>
{
    var response = await admin.PostAsJsonAsync(
        $"/api/v1/merchants/{fixture.MerchantId}/team",
        new AddMerchantTeamMemberRequest(fixture.ReadOnlyAwid, "ReadOnly"));
    Assert(response.StatusCode == HttpStatusCode.Created, "Expected 201.");
    var body = await response.Content.ReadFromJsonAsync<MerchantTeamMemberResponse>();
    Assert(body is not null && body.Role == "ReadOnly", "ReadOnly role expected.");
    Assert(body.MoneyMovementAllowed == false, "ReadOnly must never imply money movement.");
    Assert(!body.Permissions.Contains("InitiatePayout") && !body.Permissions.Contains("InitiateRefund"),
        "ReadOnly permissions must exclude money movement.");
});

await RunAsync("readonly can list team", async () =>
{
    var response = await readOnly.GetAsync($"/api/v1/merchants/{fixture.MerchantId}/team");
    Assert(response.StatusCode == HttpStatusCode.OK, "Expected 200.");
    var body = await response.Content.ReadFromJsonAsync<MerchantTeamMemberResponse[]>();
    Assert(body is not null && body.Length >= 3, "Expected owner, admin and readonly members.");
});

await RunAsync("readonly cannot manage team", async () =>
{
    var response = await readOnly.PostAsJsonAsync(
        $"/api/v1/merchants/{fixture.MerchantId}/team",
        new AddMerchantTeamMemberRequest(fixture.ForeignAwid, "Support"));
    Assert(response.StatusCode == HttpStatusCode.NotFound, "Unauthorized merchant access must be hidden.");
});

await RunAsync("foreign authenticated user is hidden", async () =>
{
    var response = await foreign.GetAsync($"/api/v1/merchants/{fixture.MerchantId}/team");
    Assert(response.StatusCode == HttpStatusCode.NotFound, "Expected 404.");
});

await RunAsync("authenticated user without active AfWal ID is forbidden", async () =>
{
    var response = await noIdentity.GetAsync($"/api/v1/merchants/{fixture.MerchantId}/team");
    Assert(response.StatusCode == HttpStatusCode.Forbidden, "Expected 403.");
});

await RunAsync("unknown target AfWal ID is rejected", async () =>
{
    var response = await owner.PostAsJsonAsync(
        $"/api/v1/merchants/{fixture.MerchantId}/team",
        new AddMerchantTeamMemberRequest("missing.afwal", "Support"));
    Assert(response.StatusCode == HttpStatusCode.NotFound, "Expected 404.");
});

await RunAsync("owner role cannot be assigned through MTA", async () =>
{
    var response = await owner.PostAsJsonAsync(
        $"/api/v1/merchants/{fixture.MerchantId}/team",
        new AddMerchantTeamMemberRequest(fixture.ForeignAwid, "Owner"));
    Assert(response.StatusCode == HttpStatusCode.BadRequest, "Expected 400.");
});

await RunAsync("owner cannot be revoked", async () =>
{
    var response = await owner.PostAsync(
        $"/api/v1/merchants/{fixture.MerchantId}/team/{Uri.EscapeDataString(fixture.OwnerAwid)}/revoke",
        null);
    Assert(response.StatusCode == HttpStatusCode.Conflict, "Expected 409.");
});

Console.WriteLine("AFW-BE-MERCHANT-TEAM-ACCESS-API-1 protected HTTP scenarios: PASS");

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class MerchantTeamFixture : IAsyncDisposable
{
    private readonly string registryDbPath;
    private readonly string teamDbPath;
    private readonly string directoryDbPath;

    private MerchantTeamFixture(
        WebApplication app,
        string registryDbPath,
        string teamDbPath,
        string directoryDbPath,
        string merchantId,
        Guid ownerUserId,
        Guid adminUserId,
        Guid readOnlyUserId,
        Guid foreignUserId,
        Guid noIdentityUserId)
    {
        App = app;
        this.registryDbPath = registryDbPath;
        this.teamDbPath = teamDbPath;
        this.directoryDbPath = directoryDbPath;
        MerchantId = merchantId;
        OwnerUserId = ownerUserId;
        AdminUserId = adminUserId;
        ReadOnlyUserId = readOnlyUserId;
        ForeignUserId = foreignUserId;
        NoIdentityUserId = noIdentityUserId;
    }

    public WebApplication App { get; }
    public string MerchantId { get; }
    public Guid OwnerUserId { get; }
    public Guid AdminUserId { get; }
    public Guid ReadOnlyUserId { get; }
    public Guid ForeignUserId { get; }
    public Guid NoIdentityUserId { get; }

    public string OwnerAwid => "owner.team.afwal";
    public string AdminAwid => "admin.team.afwal";
    public string ReadOnlyAwid => "readonly.team.afwal";
    public string ForeignAwid => "foreign.team.afwal";

    public static async Task<MerchantTeamFixture> CreateAsync()
    {
        var registryDbPath = Path.Combine(Path.GetTempPath(), $"afwal-mta-registry-{Guid.NewGuid():N}.db");
        var teamDbPath = Path.Combine(Path.GetTempPath(), $"afwal-mta-team-{Guid.NewGuid():N}.db");
        var directoryDbPath = Path.Combine(Path.GetTempPath(), $"afwal-mta-directory-{Guid.NewGuid():N}.db");

        var ownerUserId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var readOnlyUserId = Guid.NewGuid();
        var foreignUserId = Guid.NewGuid();
        var noIdentityUserId = Guid.NewGuid();

        var registryOptions = new DbContextOptionsBuilder<MerchantRegistryDbContext>()
            .UseSqlite($"Data Source={registryDbPath}").Options;
        var teamOptions = new DbContextOptionsBuilder<MerchantTeamAccessDbContext>()
            .UseSqlite($"Data Source={teamDbPath}").Options;
        var directoryOptions = new DbContextOptionsBuilder<RecipientDirectoryDbContext>()
            .UseSqlite($"Data Source={directoryDbPath}").Options;

        string merchantId;

        await using (var db = new MerchantRegistryDbContext(registryOptions))
        {
            await db.Database.EnsureCreatedAsync();
            var repository = new EfMerchantRepository(db);
            var merchant = new Merchant(
                new MerchantId("AFM-TEAMACCESS0001"),
                "owner.team.afwal",
                new BusinessProfile(
                    "Team Access Merchant GmbH",
                    "Team Merchant",
                    MerchantType.Company,
                    "DE",
                    "EUR",
                    "Technology",
                    null,
                    null,
                    new BusinessAddress("Teamstrasse 1", null, "Solingen", "42651", "DE"),
                    new MerchantContact("team@example.com", null)),
                new DateTimeOffset(2026, 9, 23, 21, 0, 0, TimeSpan.Zero));
            await repository.AddAsync(merchant);
            merchantId = merchant.MerchantId.Value;
        }

        await using (var db = new MerchantTeamAccessDbContext(teamOptions))
            await db.Database.EnsureCreatedAsync();

        await using (var db = new RecipientDirectoryDbContext(directoryOptions))
        {
            await db.Database.EnsureCreatedAsync();
            db.AfWalIdentities.AddRange(
                new AfWalIdentityEntry { Id = Guid.NewGuid(), OwnerId = ownerUserId, AfWalId = "owner.team.afwal", IsActive = true },
                new AfWalIdentityEntry { Id = Guid.NewGuid(), OwnerId = adminUserId, AfWalId = "admin.team.afwal", IsActive = true },
                new AfWalIdentityEntry { Id = Guid.NewGuid(), OwnerId = readOnlyUserId, AfWalId = "readonly.team.afwal", IsActive = true },
                new AfWalIdentityEntry { Id = Guid.NewGuid(), OwnerId = foreignUserId, AfWalId = "foreign.team.afwal", IsActive = true });
            await db.SaveChangesAsync();
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        builder.Services.AddDbContext<MerchantRegistryDbContext>(o => o.UseSqlite($"Data Source={registryDbPath}"));
        builder.Services.AddDbContext<MerchantTeamAccessDbContext>(o => o.UseSqlite($"Data Source={teamDbPath}"));
        builder.Services.AddDbContext<RecipientDirectoryDbContext>(o => o.UseSqlite($"Data Source={directoryDbPath}"));

        builder.Services.AddScoped<IMerchantRepository, EfMerchantRepository>();
        builder.Services.AddScoped<IMerchantOwnerReader, MerchantRegistryOwnerReader>();
        builder.Services.AddScoped<IMerchantTeamRepository, EfMerchantTeamRepository>();
        builder.Services.AddScoped<IMerchantTeamAuditStore, EfMerchantTeamAuditStore>();
        builder.Services.AddScoped<MerchantTeamAccessService>();
        builder.Services.AddScoped<IAfWalIdentityDirectory, EfAfWalIdentityDirectory>();
        builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(
            new DateTimeOffset(2026, 9, 23, 21, 5, 0, TimeSpan.Zero)));

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapMerchantTeamAccessEndpoints();
        await app.StartAsync();

        return new MerchantTeamFixture(
            app, registryDbPath, teamDbPath, directoryDbPath, merchantId,
            ownerUserId, adminUserId, readOnlyUserId, foreignUserId, noIdentityUserId);
    }

    public HttpClient CreateClient(Guid userId)
    {
        var client = App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
        foreach (var path in new[] { registryDbPath, teamDbPath, directoryDbPath })
            if (File.Exists(path)) File.Delete(path);
    }
}

sealed class HeaderAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var rawUser) ||
            !Guid.TryParse(rawUser.ToString(), out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity([new Claim("sub", userId.ToString())], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
