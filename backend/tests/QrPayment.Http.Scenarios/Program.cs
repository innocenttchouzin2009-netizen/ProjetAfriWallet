using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Merchant.Api.QrPayments;
using AfriWallet.Merchant.Application.Contracts.QrPayments;
using AfriWallet.Merchant.Application.QrPayments;
using AfriWallet.Merchant.Application.Services;
using AfriWallet.Merchant.Domain.Entities;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var owner = Guid.Parse("11111111-2222-3333-4444-555555555555");
var foreign = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
var ownerWalletId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");

await using var app = await BuildAppAsync(owner, ownerWalletId);
var qrService = app.Services.GetRequiredService<QrPaymentService>();
var generated = qrService.GenerateQr(
    new GenerateQrCommand(
        "merchant-001",
        QrPaymentType.Static,
        7500m,
        "XAF",
        "AfWal Market",
        "Order 42"));

var anonymous = app.GetTestClient();
var ownerClient = CreateClient(app, owner);
var foreignClient = CreateClient(app, foreign);

await RunAsync("decode returns authoritative qrId", async () =>
{
    var response = await anonymous.PostAsJsonAsync(
        "/api/v1/qr-payments/decode",
        new DecodeQrRequest(generated.Code));

    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var decoded = await response.Content.ReadFromJsonAsync<DecodedQrResponse>();
    Assert(decoded is not null && decoded.QrId == generated.QrId, "Decode must return the stored qrId.");
});

var request = new InitiateQrPaymentHttpRequest(
    generated.QrId,
    ownerWalletId.ToString(),
    7500,
    "XAF",
    "idem-http-001");

await RunAsync("anonymous initiation is rejected", async () =>
{
    var response = await anonymous.PostAsJsonAsync("/api/v1/qr-payments/initiate", request);
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("foreign wallet ownership is concealed", async () =>
{
    var response = await foreignClient.PostAsJsonAsync("/api/v1/qr-payments/initiate", request);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

QrPaymentStatusResponse? initiated = null;

await RunAsync("authenticated owner can initiate QR payment", async () =>
{
    var response = await ownerClient.PostAsJsonAsync("/api/v1/qr-payments/initiate", request);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    initiated = await response.Content.ReadFromJsonAsync<QrPaymentStatusResponse>();
    Assert(initiated is not null && !string.IsNullOrWhiteSpace(initiated.TransferIntentId), "Transfer id is required.");
});

await RunAsync("identical initiation is idempotent", async () =>
{
    var response = await ownerClient.PostAsJsonAsync("/api/v1/qr-payments/initiate", request);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var replay = await response.Content.ReadFromJsonAsync<QrPaymentStatusResponse>();
    Assert(replay?.TransferIntentId == initiated?.TransferIntentId, "Idempotent replay must preserve transfer id.");
});

await RunAsync("idempotency mismatch returns 409", async () =>
{
    var response = await ownerClient.PostAsJsonAsync(
        "/api/v1/qr-payments/initiate",
        request with { AmountMinor = 7600 });
    Assert(response.StatusCode == HttpStatusCode.Conflict, $"Expected 409, got {(int)response.StatusCode}.");
});

await RunAsync("anonymous status read is rejected", async () =>
{
    var response = await anonymous.GetAsync(
        $"/api/v1/qr-payments/transfers/{initiated!.TransferIntentId}/status");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("foreign status read is concealed", async () =>
{
    var response = await foreignClient.GetAsync(
        $"/api/v1/qr-payments/transfers/{initiated!.TransferIntentId}/status");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("owner status endpoint is read-only", async () =>
{
    var path = $"/api/v1/qr-payments/transfers/{initiated!.TransferIntentId}/status";
    var first = await ownerClient.GetFromJsonAsync<QrPaymentStatusResponse>(path);
    var second = await ownerClient.GetFromJsonAsync<QrPaymentStatusResponse>(path);

    Assert(first is not null && second is not null, "Status responses are required.");
    Assert(first == second, "Repeated status reads must be identical.");
    Assert(first.Status == QrPaymentContractValues.Statuses.Initiated, "Status read must not promote payment state.");
});

Console.WriteLine("QR payment HTTP authentication/idempotency/status scenarios passed.");

static async Task<WebApplication> BuildAppAsync(Guid owner, Guid walletId)
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();

    builder.Services
        .AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
    builder.Services.AddAuthorization();

    var wallets = new InMemoryWalletRepository();
    await wallets.AddAsync(
        Wallet.Create(
            WalletId.From(walletId),
            owner,
            Currency.From("XAF"),
            CountryCode.From("CM"),
            DateTimeOffset.UtcNow));

    builder.Services.AddSingleton<IWalletRepository>(wallets);
    builder.Services.AddSingleton<QrPaymentService>();
    builder.Services.AddSingleton<QrPaymentContractService>();

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapQrPaymentContractEndpoints();
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

sealed class InMemoryWalletRepository : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> _wallets = new();

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_wallets.Values.Any(wallet =>
            wallet.OwnerId == ownerId &&
            wallet.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        _wallets[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default)
    {
        _wallets.TryGetValue(walletId.Value, out var wallet);
        return Task.FromResult(wallet);
    }

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(
            _wallets.Values.Where(wallet => wallet.OwnerId == ownerId).ToArray());

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
        if (!Request.Headers.TryGetValue("X-Test-User", out var raw) ||
            !Guid.TryParse(raw.ToString(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim("sub", userId.ToString()) },
            Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(
            AuthenticateResult.Success(
                new AuthenticationTicket(principal, Scheme.Name)));
    }
}
