using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.P2P.Application;
using AfriWallet.P2P.Infrastructure;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.P2P;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await P2PHttpFixture.CreateAsync(configureRecipientProviders: true);
var anonymous = fixture.App.GetTestClient();
var ownerClient = CreateClient(fixture.App, fixture.OwnerId);

await RunAsync("P2P transfer requires authentication", async () =>
{
    var response = await anonymous.PostAsJsonAsync("/api/v1/p2p/transfers", fixture.AfWalRequest(Guid.NewGuid()));
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("foreign source wallet is hidden with 404", async () =>
{
    var request = fixture.AfWalRequest(Guid.NewGuid()) with { SourceWalletId = fixture.ForeignSourceWalletId };
    var response = await ownerClient.PostAsJsonAsync("/api/v1/p2p/transfers", request);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
    var error = await response.Content.ReadFromJsonAsync<P2PErrorResponse>();
    Assert(error?.Code == P2PErrorCode.NotFound, "Expected P2P_NOT_FOUND.");
});

await RunAsync("unsupported recipient kind returns 400", async () =>
{
    var request = fixture.AfWalRequest(Guid.NewGuid()) with { RecipientKind = "phone" };
    var response = await ownerClient.PostAsJsonAsync("/api/v1/p2p/transfers", request);
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
    var error = await response.Content.ReadFromJsonAsync<P2PErrorResponse>();
    Assert(error?.Code == P2PErrorCode.ValidationError, "Expected P2P_VALIDATION_ERROR.");
});

await RunAsync("unknown AfWal ID returns recipient 404", async () =>
{
    var request = fixture.AfWalRequest(Guid.NewGuid()) with { RecipientValue = "missing.id" };
    var response = await ownerClient.PostAsJsonAsync("/api/v1/p2p/transfers", request);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
    var error = await response.Content.ReadFromJsonAsync<P2PErrorResponse>();
    Assert(error?.Code == P2PErrorCode.RecipientNotFound, "Expected P2P_RECIPIENT_NOT_FOUND.");
});

await RunAsync("owned source executes AfWal ID P2P transfer", async () =>
{
    var correlationId = Guid.NewGuid();
    var response = await ownerClient.PostAsJsonAsync("/api/v1/p2p/transfers", fixture.AfWalRequest(correlationId));
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");

    var result = await response.Content.ReadFromJsonAsync<P2PTransferResponse>();
    Assert(result is not null, "P2P response is required.");
    Assert(result!.SourceWalletId == fixture.SourceWalletId, "Source wallet mismatch.");
    Assert(result.TargetWalletId == fixture.TargetWalletId, "Target wallet mismatch.");
    Assert(result.CurrencyCode == "XAF", "Currency mismatch.");
    Assert(result.AmountMinor == 2_500, "Amount mismatch.");
    Assert(result.CorrelationId == correlationId, "Correlation mismatch.");
    Assert(result.RecipientKind == "afwal-id", "Recipient kind mismatch.");
});

await RunAsync("owned source executes QR P2P transfer", async () =>
{
    var correlationId = Guid.NewGuid();
    var response = await ownerClient.PostAsJsonAsync("/api/v1/p2p/transfers", fixture.QrRequest(correlationId));
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var result = await response.Content.ReadFromJsonAsync<P2PTransferResponse>();
    Assert(result?.TargetWalletId == fixture.TargetWalletId, "QR target wallet mismatch.");
    Assert(result?.RecipientKind == "qr", "QR recipient kind mismatch.");
});

await using (var unavailable = await P2PHttpFixture.CreateAsync(configureRecipientProviders: false))
{
    var client = CreateClient(unavailable.App, unavailable.OwnerId);
    await RunAsync("missing authoritative recipient providers returns 503", async () =>
    {
        var response = await client.PostAsJsonAsync("/api/v1/p2p/transfers", unavailable.AfWalRequest(Guid.NewGuid()));
        Assert(response.StatusCode == HttpStatusCode.ServiceUnavailable, $"Expected 503, got {(int)response.StatusCode}.");
        var error = await response.Content.ReadFromJsonAsync<P2PErrorResponse>();
        Assert(error?.Code == P2PErrorCode.RecipientProvidersUnavailable, "Expected P2P_RECIPIENT_PROVIDERS_UNAVAILABLE.");
    });
}

Console.WriteLine("P2P HTTP security, ownership and composition scenarios passed.");

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
    if (!condition) throw new InvalidOperationException(message);
}

sealed class P2PHttpFixture : IAsyncDisposable
{
    private P2PHttpFixture(
        WebApplication app,
        Guid ownerId,
        Guid sourceWalletId,
        Guid foreignSourceWalletId,
        Guid targetWalletId)
    {
        App = app;
        OwnerId = ownerId;
        SourceWalletId = sourceWalletId;
        ForeignSourceWalletId = foreignSourceWalletId;
        TargetWalletId = targetWalletId;
    }

    public WebApplication App { get; }
    public Guid OwnerId { get; }
    public Guid SourceWalletId { get; }
    public Guid ForeignSourceWalletId { get; }
    public Guid TargetWalletId { get; }

    public P2PTransferRequest AfWalRequest(Guid correlationId) =>
        new(SourceWalletId, "afwal-id", "recipient.one", "XAF", 2_500, correlationId);

    public P2PTransferRequest QrRequest(Guid correlationId) =>
        new(SourceWalletId, "qr", "opaque-qr-token-1", "XAF", 500, correlationId);

    public static async Task<P2PHttpFixture> CreateAsync(bool configureRecipientProviders)
    {
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var recipientOwnerId = Guid.NewGuid();
        var sourceWalletId = Guid.NewGuid();
        var foreignSourceWalletId = Guid.NewGuid();
        var targetWalletId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var wallets = new InMemoryWalletRepository([
            Wallet.Create(WalletId.From(sourceWalletId), ownerId, Currency.Create("XAF"), null, now),
            Wallet.Create(WalletId.From(foreignSourceWalletId), otherOwnerId, Currency.Create("XAF"), null, now),
            Wallet.Create(WalletId.From(targetWalletId), recipientOwnerId, Currency.Create("XAF"), null, now)
        ]);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IWalletRepository>(wallets);
        builder.Services.AddP2PCore();

        Assert(
            builder.Services.Any(descriptor =>
                descriptor.ServiceType == typeof(IP2PTransferPort) &&
                descriptor.ImplementationType == typeof(InternalTransferP2PPort)),
            "P2P core must wire IP2PTransferPort to InternalTransferP2PPort before host overrides used by this HTTP harness.");

        if (configureRecipientProviders)
        {
            builder.Services.AddP2PRecipientDirectoryProviders(
                new FakeAfWalIdentityDirectory(new Dictionary<string, Guid>(StringComparer.Ordinal)
                {
                    ["recipient.one"] = recipientOwnerId
                }),
                new FakeQrRecipientDirectory(new Dictionary<string, Guid>(StringComparer.Ordinal)
                {
                    ["opaque-qr-token-1"] = recipientOwnerId
                }));
        }

        builder.Services.AddSingleton<RecordingP2PTransferPort>();
        builder.Services.AddSingleton<IP2PTransferPort>(services => services.GetRequiredService<RecordingP2PTransferPort>());

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapP2PEndpoints();
        await app.StartAsync();

        return new P2PHttpFixture(app, ownerId, sourceWalletId, foreignSourceWalletId, targetWalletId);
    }

    public async ValueTask DisposeAsync() => await App.DisposeAsync();
}

sealed class RecordingP2PTransferPort : IP2PTransferPort
{
    public Task<P2PTransferReceipt> ExecuteAsync(
        Guid sourceWalletId,
        Guid targetWalletId,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new P2PTransferReceipt(
            Guid.NewGuid(),
            sourceWalletId,
            targetWalletId,
            "XAF",
            amountMinor,
            correlationId,
            requestedAtUtc));
    }
}

sealed class FakeAfWalIdentityDirectory(IReadOnlyDictionary<string, Guid> entries) : IAfWalIdentityDirectory
{
    public Task<Guid?> ResolveOwnerIdAsync(string afWalId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(entries.TryGetValue(afWalId, out var ownerId) ? (Guid?)ownerId : null);
    }
}

sealed class FakeQrRecipientDirectory(IReadOnlyDictionary<string, Guid> entries) : IQrRecipientDirectory
{
    public Task<Guid?> ResolveOwnerIdAsync(string qrToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(entries.TryGetValue(qrToken, out var ownerId) ? (Guid?)ownerId : null);
    }
}

sealed class InMemoryWalletRepository(IEnumerable<Wallet> wallets) : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> values = wallets.ToDictionary(wallet => wallet.Id.Value);

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.Values.Any(wallet => wallet.OwnerId == ownerId && wallet.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        values[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.TryGetValue(walletId.Value, out var wallet) ? wallet : null);

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(values.Values.Where(wallet => wallet.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        values[wallet.Id.Value] = wallet;
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

        var identity = new ClaimsIdentity([new Claim("sub", userId.ToString())], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
