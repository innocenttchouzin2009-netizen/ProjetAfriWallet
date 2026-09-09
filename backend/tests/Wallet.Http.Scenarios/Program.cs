using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.Wallet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var ownerA = Guid.NewGuid();
var ownerB = Guid.NewGuid();

await using var app = await BuildAppAsync();
var anonymous = app.GetTestClient();

await RunAsync("unauthenticated wallet access returns 401", async () =>
{
    var response = await anonymous.GetAsync("/api/v1/wallets");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

var clientA = CreateClient(app, ownerA);
var clientB = CreateClient(app, ownerB);
Guid walletId = Guid.Empty;

await RunAsync("owner can create wallet", async () =>
{
    var response = await clientA.PostAsJsonAsync("/api/v1/wallets", new { currencyCode = "XAF", countryCode = "CM" });
    Assert(response.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)response.StatusCode}.");
    var wallet = await response.Content.ReadFromJsonAsync<WalletView>();
    Assert(wallet is not null, "Created wallet response is required.");
    Assert(wallet!.OwnerId == ownerA, "Wallet owner must come from authenticated sub claim.");
    walletId = wallet.WalletId;
});

await RunAsync("duplicate wallet is rejected", async () =>
{
    var response = await clientA.PostAsJsonAsync("/api/v1/wallets", new { currencyCode = "xaf", countryCode = "cm" });
    Assert(response.StatusCode == HttpStatusCode.Conflict, $"Expected 409, got {(int)response.StatusCode}.");
});

await RunAsync("unsupported currency is rejected", async () =>
{
    var response = await clientA.PostAsJsonAsync("/api/v1/wallets", new { currencyCode = "GBP", countryCode = "GB" });
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
});

await RunAsync("wallet ownership is concealed from another user", async () =>
{
    var get = await clientB.GetAsync($"/api/v1/wallets/{walletId}");
    Assert(get.StatusCode == HttpStatusCode.NotFound, $"Expected 404 for foreign wallet, got {(int)get.StatusCode}.");

    var list = await clientB.GetFromJsonAsync<WalletView[]>("/api/v1/wallets");
    Assert(list is not null && list.Length == 0, "Second user must not see first user's wallets.");

    var close = await clientB.PostAsync($"/api/v1/wallets/{walletId}/close", null);
    Assert(close.StatusCode == HttpStatusCode.NotFound, $"Expected 404 for foreign lifecycle action, got {(int)close.StatusCode}.");
});

await RunAsync("closed wallet lifecycle is terminal", async () =>
{
    var close = await clientA.PostAsync($"/api/v1/wallets/{walletId}/close", null);
    Assert(close.StatusCode == HttpStatusCode.OK, $"Expected 200 on close, got {(int)close.StatusCode}.");

    var activate = await clientA.PostAsync($"/api/v1/wallets/{walletId}/activate", null);
    Assert(activate.StatusCode == HttpStatusCode.Conflict, $"Expected 409 after terminal close, got {(int)activate.StatusCode}.");
});

await RunAsync("owner list remains isolated", async () =>
{
    var listA = await clientA.GetFromJsonAsync<WalletView[]>("/api/v1/wallets");
    var listB = await clientB.GetFromJsonAsync<WalletView[]>("/api/v1/wallets");
    Assert(listA is not null && listA.Length == 1 && listA[0].WalletId == walletId, "Owner A must see exactly own wallet.");
    Assert(listB is not null && listB.Length == 0, "Owner B must remain isolated.");
});

Console.WriteLine("Wallet HTTP security & integration scenarios passed.");

static async Task<WebApplication> BuildAppAsync()
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();

    builder.Services
        .AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
    builder.Services.AddAuthorization();
    builder.Services.AddSingleton<IWalletRepository, InMemoryWalletRepository>();
    builder.Services.AddSingleton<ISupportedCurrencyPolicy>(new FixedSupportedCurrencyPolicy("XAF", "EUR", "USD"));
    builder.Services.AddScoped<WalletRegistryApplicationService>();

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapWalletEndpoints();
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

sealed class FixedSupportedCurrencyPolicy(params string[] supported) : ISupportedCurrencyPolicy
{
    private readonly HashSet<string> _supported = supported.ToHashSet(StringComparer.OrdinalIgnoreCase);
    public bool IsSupported(string currencyCode) => _supported.Contains(currencyCode);
}

sealed class InMemoryWalletRepository : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> _wallets = new();

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_wallets.Values.Any(wallet => wallet.OwnerId == ownerId && wallet.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        _wallets.Add(wallet.Id.Value, wallet);
        return Task.CompletedTask;
    }

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default)
    {
        _wallets.TryGetValue(walletId.Value, out var wallet);
        return Task.FromResult(wallet);
    }

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(_wallets.Values.Where(wallet => wallet.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        _wallets[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
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
        if (!Request.Headers.TryGetValue("X-Test-User", out var raw) || !Guid.TryParse(raw.ToString(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
